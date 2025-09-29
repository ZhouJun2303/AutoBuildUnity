pipeline {
    agent any

    environment {
        BUILD_OUTPUT_PATH = "C:\\IIS_ServerData\\${JOB_BASE_NAME}\\BuildOutput\\V${VERSION_CODE}\\"
        UNITY_LOG_PATH = "C:\\IIS_ServerData\\${JOB_BASE_NAME}\\UnityLog\\V${VERSION_CODE}\\"
    }

    stages {

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
                    // 获取当前时间
                    def currentTime = new Date().format('yyyy_MM_dd_HH_mm_ss')
                    env.CURRENT_TIME = currentTime
                    echo "当前时间: ${currentTime}"

                    // 打印环境变量
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

                    // 判断并创建目录
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
                        def apkName = "${env.JOB_BASE_NAME}_${env.VERSION_CODE}_Release_${env.CURRENT_TIME}"
                        def apkFullName = "${env.JOB_BASE_NAME}_${env.VERSION_CODE}_Release_${env.CURRENT_TIME}.apk"
                        def source = "${env.ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\apk\\release\\${apkFullName}"
                        def dest = "${env.BUILD_OUTPUT_PATH}${apkName}_signed.apk"

                        try {
                            // 1️⃣ 构建 APK
                            echo "===== 开始构建 Release APK ====="
                            bat "gradlew.bat assembleRelease -PcustomName=${apkName} -PversionCode=${env.VERSION_CODE} -PversionName=${env.VERSION_NAME} --stacktrace"

                            // 构建完成后列出 release 目录内容
                            def releaseDir = "${env.ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\apk\\release"
                            bat "echo ===== Release 目录文件列表 ====="
                            bat "dir /b \"${releaseDir}\""

                            bat """
                                if not exist "${source}" (
                                    echo ERROR: 源 APK 文件不存在
                                    exit /b 1
                                )
                            """

                            echo "Source APK: ${source}"
                            echo "===== 构建完成 ====="

                            // 2️⃣ 签名 APK
                            echo "===== 开始签名 ====="
                            bat "call \"${env.APKSIGNER}\" sign --ks \"${env.storeFilefile}\" --ks-pass pass:${env.storePassword} --key-pass pass:${env.keyPassword} --ks-key-alias ${env.keyAlias} --in \"${source}\" --out \"${source}\""
                            echo "===== 签名完成 ====="

                            // 3️⃣ 拷贝到输出目录
                            bat "if not exist \"${env.BUILD_OUTPUT_PATH}\" mkdir \"${env.BUILD_OUTPUT_PATH}\""
                            bat "copy /y \"${source}\" \"${dest}\""
                            echo "拷贝完成: ${dest}"

                        } catch (err) {
                            echo "===== 构建 Release APK 阶段失败 ====="
                            throw err
                        } finally {
                            echo "===== 构建 Release APK 阶段结束 ====="
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
                        def apkName = "${env.JOB_BASE_NAME}_${env.VERSION_CODE}_Debug_${env.CURRENT_TIME}"
                        def apkFullName = "${apkName}.apk"
                        def source = "${env.ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\apk\\debug\\${apkFullName}"
                        def dest = "${env.BUILD_OUTPUT_PATH}${apkName}_signed.apk"

                        try {
                            // 1️⃣ 构建 Debug APK
                            echo "===== 开始构建 Debug APK ====="
                            bat "gradlew.bat assembleDebug -PcustomName=${apkName} -PversionCode=${env.VERSION_CODE} -PversionName=${env.VERSION_NAME} --stacktrace"

                            // 构建完成后列出 debug 目录内容
                            def debugDir = "${env.ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\apk\\debug"
                            bat "echo ===== Debug 目录文件列表 ====="
                            bat "dir /b \"${debugDir}\""

                            // 校验 APK 是否存在
                            bat """
                                if not exist "${source}" (
                                    echo ERROR: 源 Debug APK 文件不存在
                                    exit /b 1
                                )
                            """
                            echo "Source Debug APK: ${source}"
                            echo "===== 构建完成 ====="

                            // 2️⃣ 签名 Debug APK
                            echo "===== 开始签名 Debug APK ====="
                            bat "call \"${env.APKSIGNER}\" sign --ks \"${env.storeFilefile}\" --ks-pass pass:${env.storePassword} --key-pass pass:${env.keyPassword} --ks-key-alias ${env.keyAlias} --in \"${source}\" --out \"${source}\""
                            echo "===== 签名完成 ====="

                            // 3️⃣ 拷贝到输出目录
                            bat "if not exist \"${env.BUILD_OUTPUT_PATH}\" mkdir \"${env.BUILD_OUTPUT_PATH}\""
                            bat "copy /y \"${source}\" \"${dest}\""
                            echo "拷贝完成: ${dest}"

                        } catch (err) {
                            echo "===== 构建 Debug APK 阶段失败 ====="
                            throw err
                        } finally {
                            echo "===== 构建 Debug APK 阶段结束 ====="
                        }
                    }
                }
            }
        }

        stage('Build Release AAB') {
            when { expression { BUILD_ANDROID_AAB == "true"} }
            steps {
                script {
                    dir("${env.ANDROID_PROJECT_PATH}") {
                        def aabName = "${env.JOB_BASE_NAME}_${env.VERSION_CODE}__AAB_Release_${env.CURRENT_TIME}"
                        def aabFullName = "${env.JOB_BASE_NAME}_${env.VERSION_CODE}__AAB_Release_${env.CURRENT_TIME}.aab"
                        def source = "${env.ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\bundle\\release\\${aabName}-release.aab"
                        def dest = "${env.BUILD_OUTPUT_PATH}${aabFullName}"
                        try {
                            // 1️⃣ 构建 Release AAB
                            echo "===== 开始构建 Release AAB ====="
                            bat "gradlew.bat bundleRelease -PcustomName=${aabName} -PversionCode=${env.VERSION_CODE} -PversionName=${env.VERSION_NAME} --stacktrace"

                            // 构建完成后列出 release 目录内容
                            def releaseDir = "${env.ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\bundle\\release"
                            bat "echo ===== Release AAB 目录文件列表 ====="
                            bat "dir /b \"${releaseDir}\""

                            // 校验 AAB 是否存在
                            bat """
                                if not exist "${source}" (
                                    echo ERROR: 源 Release AAB 文件不存在
                                    exit /b 1
                                )
                            """
                            echo "Source Release AAB: ${source}"
                            echo "===== 构建完成 ====="

                            // 2️⃣ 签名 AAB（可选，通常不签名 AAB，这里按你的习惯保留）
                            // echo "===== 开始签名 Release AAB ====="
                            // bat "call \"${env.APKSIGNER}\" sign --ks \"${env.storeFilefile}\" --ks-pass pass:${env.storePassword} --key-pass pass:${env.keyPassword} --ks-key-alias ${env.keyAlias} --in \"${source}\" --out \"${source}\""
                            // echo "===== 签名完成 ====="

                            // 3️⃣ 拷贝到输出目录
                            bat "if not exist \"${env.BUILD_OUTPUT_PATH}\" mkdir \"${env.BUILD_OUTPUT_PATH}\""
                            bat "copy /y \"${source}\" \"${dest}\""
                            echo "拷贝完成: ${dest}"

                        } catch (err) {
                            echo "===== 构建 Release AAB 阶段失败 ====="
                            throw err
                        } finally {
                            echo "===== 构建 Release AAB 阶段结束 ====="
                        }
                    }
                }
            }
        }

        stage('Build Debug AAB') {
            when { expression { BUILD_ANDROID_AAB == "true" && ONLY_RELEASE == "false"} }
            steps {
                script {
                    dir("${env.ANDROID_PROJECT_PATH}") {
                        def aabName = "${env.JOB_BASE_NAME}_${env.VERSION_CODE}__AAB_Debug_${env.CURRENT_TIME}"
                        def aabFullName = "${env.JOB_BASE_NAME}_${env.VERSION_CODE}__AAB_Debug_${env.CURRENT_TIME}.aab"
                        def source = "${env.ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\bundle\\debug\\${aabName}-debug.aab"
                        def dest = "${env.BUILD_OUTPUT_PATH}${aabFullName}"

                        try {
                            // 1️⃣ 构建 Debug AAB
                            echo "===== 开始构建 Debug AAB ====="
                            bat "gradlew.bat bundleDebug -PcustomName=${aabName} -PversionCode=${env.VERSION_CODE} -PversionName=${env.VERSION_NAME} --stacktrace"

                            // 构建完成后列出 debug 目录内容
                            def debugDir = "${env.ANDROID_PROJECT_PATH}\\launcher\\build\\outputs\\bundle\\debug"
                            bat "echo ===== Debug AAB 目录文件列表 ====="
                            bat "dir /b \"${debugDir}\""

                            // 校验 AAB 是否存在
                            bat """
                                if not exist "${source}" (
                                    echo ERROR: 源 Debug AAB 文件不存在
                                    exit /b 1
                                )
                            """
                            echo "Source Debug AAB: ${source}"
                            echo "===== 构建完成 ====="

                            // 2️⃣ 签名 Debug AAB（通常不签名 AAB）
                            // echo "===== 开始签名 Debug AAB ====="
                            // bat "call \"${env.APKSIGNER}\" sign --ks \"${env.storeFilefile}\" --ks-pass pass:${env.storePassword} --key-pass pass:${env.keyPassword} --ks-key-alias ${env.keyAlias} --in \"${source}\" --out \"${source}\""
                            // echo "===== 签名完成 ====="

                            // 3️⃣ 拷贝到输出目录
                            bat "if not exist \"${env.BUILD_OUTPUT_PATH}\" mkdir \"${env.BUILD_OUTPUT_PATH}\""
                            bat "copy /y \"${source}\" \"${dest}\""
                            echo "拷贝完成: ${dest}"

                        } catch (err) {
                            echo "===== 构建 Debug AAB 阶段失败 ====="
                            throw err
                        } finally {
                            echo "===== 构建 Debug AAB 阶段结束 ====="
                        }
                    }
                }
            }
        }

    }
}
