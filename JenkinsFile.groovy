pipeline {
    agent any

    environment {
        BUILD_OUTPUT_PATH = "C:\\IIS_ServerData\\${JOB_BASE_NAME}\\BuildOutput\\V${VERSION_CODE}\\"
        UNITY_LOG_PATH = "C:\\IIS_ServerData\\${JOB_BASE_NAME}\\UnityLog\\V${VERSION_CODE}\\"
    }

    stages {
        stage('Notify Build Start') {
            steps {
                script {
                    generatedFiles = []
                    sendFeishuCardMsg("构建开始", "项目: ${JOB_BASE_NAME}\n版本: ${VERSION_CODE}", '', '', '', '', 2)
                }
            }
        }

        stage('Check Environment') {
            steps {
                script {
                    bat '''
                        echo ===== 检查环境依赖 =====
                        if not exist "%APKSIGNER%" exit /b 1
                        if not exist "%UNITY_EDITOR_PATH%" exit /b 2
                        if not exist "%UNITY_PROJECT_PATH%" exit /b 3
                        if not exist "%ANDROID_PROJECT_PATH%" exit /b 4
                        echo ===== 环境检查通过 =====
                    '''
                }
            }
        }

        stage('Log And Init') {
            steps {
                script {
                    def currentTime = new Date().format('yyyy_MM_dd_HH_mm_ss')
                    env.CURRENT_TIME = currentTime

                    bat """
                        @echo JOB_BASE_NAME=%JOB_BASE_NAME%
                        @echo VERSION_CODE=%VERSION_CODE%
                        @echo VERSION_NAME=%VERSION_NAME%
                        @echo UNITY_EDITOR_PATH=%UNITY_EDITOR_PATH%
                        @echo UNITY_PROJECT_PATH=%UNITY_PROJECT_PATH%
                        @echo ANDROID_PROJECT_PATH=%ANDROID_PROJECT_PATH%
                        @echo BUILD_OUTPUT_PATH=%BUILD_OUTPUT_PATH%
                        @echo UNITY_LOG_PATH=%UNITY_LOG_PATH%
                        @echo APKSIGNER=%APKSIGNER%
                        @echo CURRENT_TIME=%CURRENT_TIME%
                    """

                    bat """
                        if not exist "%BUILD_OUTPUT_PATH%" mkdir "%BUILD_OUTPUT_PATH%"
                        if not exist "%UNITY_LOG_PATH%" mkdir "%UNITY_LOG_PATH%"
                    """
                }
            }
        }

        stage('Unity Git Sync') {
            when { expression { SYNC_UNITY_GIT == "true" } }
            steps {
                script {
                    dir("${env.UNITY_PROJECT_PATH}") {
                        bat """
                            if exist .git (
                                echo ===== Unity Git Sync =====
                                git config --global --add safe.directory %UNITY_PROJECT_PATH%
                                git checkout -- .
                                git pull
                            ) else (
                                echo [警告] %UNITY_PROJECT_PATH% 不是 Git 仓库，跳过同步
                            )
                        """
                    }
                }
            }
        }

        stage('Kill Unity') {
            when { expression { BUILD_UNITY == "true" } }
            steps {
                script {
                    bat """
                        TASKKILL /F /IM Unity.exe || echo Unity not running
                        PING 127.0.0.1 -n 3 >NUL
                    """
                }
            }
        }

        stage('Build Unity') {
            when { expression { BUILD_UNITY == "true" } }
            steps {
                timeout(time: 60, unit: 'MINUTES') {
                    script {
                        bat """
                            "%UNITY_EDITOR_PATH%" -batchmode -projectPath %UNITY_PROJECT_PATH% -executeMethod BuildProject.TestBuildSuccess -logFile %UNITY_LOG_PATH%%CURRENT_TIME%.log --productName:%JOB_BASE_NAME% --version:%VERSION_CODE% -buildTarget:Android
                        """
                    }
                }
            }
        }

        stage('Android Git Sync') {
            when { expression { SYNC_ANDROID_GIT == "true" } }
            steps {
                script {
                    dir("${env.ANDROID_PROJECT_PATH}") {
                        bat """
                            if exist .git (
                                echo ===== Android Git Sync =====
                                git config --global --add safe.directory %ANDROID_PROJECT_PATH%
                                git checkout -- .
                                git pull
                            ) else (
                                echo [警告] %ANDROID_PROJECT_PATH% 不是 Git 仓库，跳过同步
                            )
                        """
                    }
                }
            }
        }

        stage('Clean Android Cache') {
            when { expression { CLEAN_ANDROID_CACHED == "true" } }
            steps {
                script {
                    dir("${env.ANDROID_PROJECT_PATH}") {
                        bat "gradlew.bat clean"
                    }
                }
            }
        }

        // ---------------- Android 构建阶段 ----------------
        stage('Build Release APK') {
            when { expression { BUILD_ANDROID_APK == "true" } }
            steps {
                script {
                    dir(env.ANDROID_PROJECT_PATH) {
                        def apkName = "${JOB_BASE_NAME}_${VERSION_CODE}_Release_${CURRENT_TIME}"
                        def apkFullName = "${apkName}.apk"
                        def source = "${ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\apk\\release\\${apkFullName}"
                        def dest = "${BUILD_OUTPUT_PATH}${apkFullName}"
                        def serverPath = "${JOB_BASE_NAME}\\BuildOutput\\V${VERSION_CODE}\\${apkFullName}"
                        try {
                            bat "gradlew.bat assembleRelease -PcustomName=${apkName} -PversionCode=${VERSION_CODE} -PversionName=${VERSION_NAME} --stacktrace"
                            bat "copy /y \"${source}\" \"${dest}\""
                            generatedFiles << [name: apkFullName, path: serverPath]
                        } catch (err) {
                            throw err
                        }
                    }
                }
            }
        }

        stage('Build Debug APK') {
            when { expression { BUILD_ANDROID_APK == "true" && ONLY_RELEASE == "false"} }
            steps {
                script {
                    dir(env.ANDROID_PROJECT_PATH) {
                        def apkName = "${JOB_BASE_NAME}_${VERSION_CODE}_Debug_${CURRENT_TIME}"
                        def apkFullName = "${apkName}.apk"
                        def source = "${ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\apk\\debug\\${apkFullName}"
                        def dest = "${BUILD_OUTPUT_PATH}${apkFullName}"
                        def serverPath = "${JOB_BASE_NAME}\\BuildOutput\\V${VERSION_CODE}\\${apkFullName}"
                        try {
                            bat "gradlew.bat assembleDebug -PcustomName=${apkName} -PversionCode=${VERSION_CODE} -PversionName=${VERSION_NAME} --stacktrace"
                            bat "copy /y \"${source}\" \"${dest}\""
                            generatedFiles << [name: apkFullName, path: serverPath]
                        } catch (err) {
                            throw err
                        }
                    }
                }
            }
        }

        stage('Build Release AAB') {
            when { expression { BUILD_ANDROID_AAB == "true"} }
            steps {
                script {
                    dir("${ANDROID_PROJECT_PATH}") {
                        def aabName = "${JOB_BASE_NAME}_${VERSION_CODE}__AAB_Release_${CURRENT_TIME}"
                        def aabFullName = "${aabName}.aab"
                        def source = "${ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\bundle\\release\\${aabName}-release.aab"
                        def dest = "${BUILD_OUTPUT_PATH}${aabFullName}"
                        def serverPath = "${JOB_BASE_NAME}\\BuildOutput\\V${VERSION_CODE}\\${aabFullName}"
                        try {
                            bat "gradlew.bat bundleRelease -PcustomName=${aabName} -PversionCode=${VERSION_CODE} -PversionName=${VERSION_NAME} --stacktrace"
                            bat "copy /y \"${source}\" \"${dest}\""
                            generatedFiles << [name: aabFullName, path: serverPath]
                        } catch (err) {
                            throw err
                        }
                    }
                }
            }
        }

        stage('Build Debug AAB') {
            when { expression { BUILD_ANDROID_AAB == "true" && ONLY_RELEASE == "false"} }
            steps {
                script {
                    dir("${ANDROID_PROJECT_PATH}") {
                        def aabName = "${JOB_BASE_NAME}_${VERSION_CODE}__AAB_Debug_${CURRENT_TIME}"
                        def aabFullName = "${aabName}.aab"
                        def source = "${ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\bundle\\debug\\${aabName}-debug.aab"
                        def dest = "${BUILD_OUTPUT_PATH}${aabFullName}"
                        def serverPath = "${JOB_BASE_NAME}\\BuildOutput\\V${VERSION_CODE}\\${aabFullName}"
                        try {
                            bat "gradlew.bat bundleDebug -PcustomName=${aabName} -PversionCode=${VERSION_CODE} -PversionName=${VERSION_NAME} --stacktrace"
                            bat "copy /y \"${source}\" \"${dest}\""
                            generatedFiles << [name: aabFullName, path: serverPath]
                        } catch (err) {
                            throw err
                        }
                    }
                }
            }
        }

    }

    post {
        success {
            script {
                // Jenkins 自带 duration 是毫秒，转秒
                def duration = (currentBuild.duration / 1000)
                echo "构建耗时: ${duration}s"

                // 构建消息
                def msg = "构建完成，总耗时：${duration}s\n\n可下载文件：\n"
                if (generatedFiles != null && generatedFiles.size() > 0) {
                    generatedFiles.each { f ->
                        def fileName = f?.name ?: "unknown"
                        def filePath = f?.path ?: "unknown"
                        def url = "http://192.168.18.62:8866/${filePath}"
                        msg += "- [${fileName}](${url})\n"
                    }
                } else {
                    msg += "- 无文件生成\n"
                }

                // 发送飞书消息
                try {
                    sendFeishuCardMsg("构建成功", msg, '', '', '', '', 2)
                    echo "飞书消息发送成功"
                } catch (err) {
                    echo "飞书消息发送失败: ${err}"
                }
            }
        }
        failure {
            script {
                def duration = (currentBuild.duration / 1000)
                echo "构建失败，总耗时: ${duration}s"

                try {
                    sendFeishuCardMsg("构建失败", "构建失败，总耗时：${duration}s", '', '', '', '', 3)
                    echo "飞书消息发送成功"
                } catch (err) {
                    echo "飞书消息发送失败: ${err}"
                }
            }
        }
    }
}

// 飞书消息函数（打印所有参数，带容错和日志）
def sendFeishuCardMsg(headerName, message, url1='', url1Name='', url12='', url12Name='', messageType=2) {
    echo "准备发送飞书消息"
    echo "参数:"
    echo "  headerName: ${headerName}"
    echo "  message: ${message}"
    echo "  url1: ${url1}"
    echo "  url1Name: ${url1Name}"
    echo "  url12: ${url12}"
    echo "  url12Name: ${url12Name}"
    echo "  messageType: ${messageType}"

    try {
        def template = "blue"
        def btnType = "laser"
        if (messageType == 2) template = "green"
        else if (messageType == 3) { 
            template = "red"
            btnType = "danger" 
        }

        // 构造消息按钮
        def elements = [
            [
                tag: "markdown",
                content: message,
                text_align: "left",
                text_size: "normal_v2",
                margin: "0px 0px 0px 0px"
            ]
        ]
        if (url1 && url1Name) elements << [
            tag: "button",
            text: [tag:"plain_text", content:url1Name],
            type: btnType,
            width:"default",
            size:"medium",
            behaviors:[[type:"open_url", default_url:url1]]
        ]
        if (url12 && url12Name) elements << [
            tag: "button",
            text: [tag:"plain_text", content:url12Name],
            type: btnType,
            width:"default",
            size:"medium",
            behaviors:[[type:"open_url", default_url:url12]]
        ]

        def body = [
            msg_type: "interactive",
            card: [
                schema: "2.0",
                config: [update_multi:true],
                header: [
                    title:[tag:"plain_text", content:headerName],
                    template: template
                ],
                body: [
                    direction:"vertical",
                    padding:"12px",
                    elements: elements
                ]
            ]
        ]

        // 打印请求体
        def jsonBody = groovy.json.JsonOutput.prettyPrint(groovy.json.JsonOutput.toJson(body))
        echo "飞书请求体:\n${jsonBody}"

        // 发送 HTTP 请求
        def response = httpRequest(
            acceptType: 'APPLICATION_JSON',
            contentType: 'APPLICATION_JSON',
            httpMode: 'POST',
            requestBody: groovy.json.JsonOutput.toJson(body),
            url: FEISHU_WEBHOOK_URL,
            validResponseCodes: '100:599',
            consoleLogResponseBody: true
        )

        // 响应处理
        echo "飞书响应状态码: ${response.status}"
        echo "飞书响应内容: ${response.content}"
        if (response.status >= 200 && response.status < 300) {
            echo "飞书消息发送成功"
        } else {
            echo "飞书消息发送失败"
        }

    } catch (err) {
        echo "发送飞书消息异常: ${err}"
    }
}

