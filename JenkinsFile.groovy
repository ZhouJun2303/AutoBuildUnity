pipeline {
    agent any
    parameters {
        // ========================
        // ✅ 持久化参数（用户可修改，且会保存上次输入）
        // ========================
        persistentString(
            name: 'VERSION_NAME',
            defaultValue: '1.0.0',
            description: '应用版本号'
        )
        persistentString(
            name: 'VERSION_CODE',
            defaultValue: '100',
            description: '应用版本号 Code'
        )
        persistentString(
            name: 'UNITY_CUSTOME_PARAM',
            defaultValue: '1',
            description: '自定义参数，传递至 unity',
            trim: false
        )
        persistentChoice(
            name: 'SYNC_UNITY_GIT',
            choices: ['true', 'false'],
            description: '同步 Unity git'
        )
        persistentChoice(
            name: 'BUILD_UNITY',
            choices: ['true', 'false'],
            description: 'Unity 是否导出'
        )
        persistentChoice(
            name: 'SYNC_ANDROID_GIT',
            choices: ['true', 'false'],
            description: '同步 Android git'
        )
        persistentChoice(
            name: 'CLEAN_ANDROID_CACHED',
            choices: ['true', 'false'],
            description: '是否构建清除Android 缓存，清除之后构建会变慢'
        )
        persistentChoice(
            name: 'BUILD_ANDROID_APK',
            choices: ['true', 'false'],
            description: '是否构建apk'
        )
        persistentChoice(
            name: 'BUILD_ANDROID_AAB',
            choices: ['true', 'false'],
            description: '是否构建aab'
        )
        persistentChoice(
            name: 'BUILD_DEBUG',
            choices: ['true', 'false'],
            description: '是否构建附带debug包'
        )

        // ========================
        // 🔒 隐藏参数（使用 hidden plugin 或 password/string）
        // ========================
        hidden(
            name: 'storeFilefile',
            defaultValue: 'C:\\AndroidKey\\heromarking.keystore',
            description: '签名 storeFile file'
        )
        password(
            name: 'storePassword',
            defaultValue: 'heromarking20211028',
            description: '签名 storePassword'
        )
        password(
            name: 'keyAlias',
            defaultValue: 'heromarking',
            description: '签名 keyAlias'
        )
        password(
            name: 'keyPassword',
            defaultValue: 'heromarking20211028',
            description: '签名 keyPassword'
        )
        hidden(
            name: 'UNITY_EDITOR_PATH',
            defaultValue: 'D:\\Unity\\Unity 2021.3.45f1\\Editor\\Unity.exe',
            description: 'Unity 编辑器路径'
        )
        hidden(
            name: 'UNITY_PROJECT_PATH',
            defaultValue: 'D:\\MyGit\\AutoBuildUnity',
            description: 'Unity 项目路径'
        )
        hidden(
            name: 'ANDROID_PROJECT_PATH',
            defaultValue: 'D:\\MyGit\\AutoBuildUnity_Build',
            description: 'Android 项目路径'
        )
        hidden(
            name: 'APKSIGNER',
            defaultValue: 'D:\\SDK\\build-tools\\36.0.0\\apksigner.bat',
            description: 'apksigner 工具路径'
        )
        hidden(
            name: 'FEISHU_WEBHOOK_URL',
            defaultValue: 'https://open.feishu.cn/open-apis/bot/v2/hook/3447cff8-3872-4dd8-acd5-6867159b781a',
            description: '飞书 webhook 地址'
        )
    }

    environment {
        BUILD_OUTPUT_PATH = "C:\\IIS_ServerData\\${JOB_BASE_NAME}\\BuildOutput\\V${VERSION_CODE}\\"
        UNITY_LOG_PATH = "C:\\IIS_ServerData\\${JOB_BASE_NAME}\\UnityLog\\V${VERSION_CODE}\\"
        JENKINS_SERVER = "http://192.168.18.62:8867/"
        IIS_SERVER = "http://192.168.18.62:8866/"
    }

    stages {
        stage('Notify Build Start') {
            steps {
                script {
                    generatedFiles = []

                    // 获取所有持久化参数
                    def visibleParams = [
                        'VERSION_NAME', 'VERSION_CODE', 'UNITY_CUSTOME_PARAM',
                        'SYNC_UNITY_GIT', 'BUILD_UNITY', 'SYNC_ANDROID_GIT',
                        'CLEAN_ANDROID_CACHED', 'BUILD_ANDROID_APK', 'BUILD_ANDROID_AAB',
                        'BUILD_DEBUG'
                    ]

                    // 构建参数消息
                    def paramMsg = visibleParams.collect { paramName ->
                        "${paramName}: ${params[paramName]}"
                    }.join("\n")

                    // 打印到控制台
                    echo "构建开始，参数如下:\n${paramMsg}"

                    // 发送飞书消息
                    def feishuMsg = "构建开始\n项目: ${JOB_BASE_NAME}\n参数:\n${paramMsg}\n"
                    feishuMsg += "Jenkins 地址：${JENKINS_SERVER}job/${JOB_NAME}/\n"
                    sendFeishuCardMsg("构建开始", feishuMsg, '', '', '', '', 2)
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
            when { expression { params.SYNC_ANDROID_GIT == "true" } }
            steps {
                powershell """
                echo ===== Android Git Sync =====

                # 检查是否是 git 仓库
                if (Test-Path '${env.ANDROID_PROJECT_PATH}\\.git') {
                    git -C '${env.ANDROID_PROJECT_PATH}' -c safe.directory='${env.ANDROID_PROJECT_PATH}' checkout -- .
                    git -C '${env.ANDROID_PROJECT_PATH}' -c safe.directory='${env.ANDROID_PROJECT_PATH}' pull
                } else {
                    Write-Host "[警告] ${env.ANDROID_PROJECT_PATH} 不是 Git 仓库，跳过同步"
                }
                """
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
            when { expression { BUILD_ANDROID_APK == "true" && BUILD_DEBUG == "true"} }
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
            when { expression { BUILD_ANDROID_AAB == "true" && BUILD_DEBUG == "true"} }
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
                def msg = "构建完成，总耗时：${duration}s\n"
                msg+="项目: ${JOB_BASE_NAME}\n"
                msg+= "IIS 服务器：${IIS_SERVER}${JOB_BASE_NAME}\n\n"
                msg+= "可下载文件：\n"
                if (generatedFiles != null && generatedFiles.size() > 0) {
                    generatedFiles.each { f ->
                        def fileName = f?.name ?: "unknown"
                        def filePath = f?.path ?: "unknown"
                        def url = "${IIS_SERVER}${filePath}"
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

                // 构建消息
                def msg = "构建失败，总耗时：${duration}s\n\n"
                msg+="项目: ${JOB_BASE_NAME}\n"
                msg += "Jenkins 地址：${JENKINS_SERVER}job/${JOB_NAME}/\n"
                msg += "构建 console：${JENKINS_SERVER}job/${JOB_NAME}/${currentBuild.number}/console\n"
                msg += "IIS 服务器：${IIS_SERVER}${JOB_BASE_NAME}\n"

                try {
                    sendFeishuCardMsg("构建失败", msg, '', '', '', '', 3)
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
        // echo "飞书请求体:\n${jsonBody}"

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

