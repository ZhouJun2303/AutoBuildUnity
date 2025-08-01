pipeline {
    agent any
    environment {
        //项目名称
        PROJECT_NAME = 'PipelineTEST'
        //Unity 编辑器路径
        // UNITY_EDITOR_PATH = "C:\\Program Files\\Unity\\Hub\\Editor\\2021.3.20f1\\Editor\\Unity.exe"
        UNITY_EDITOR_PATH = 'C:\\Unity\\UnityEditor\\Unity 2020.3.33f1\\Editor\\Unity.exe'
        //Unity 项目路径
        UNITY_PROJECT_PATH = 'C:\\MyGit\\AutoBuildUnity'
        //Android 项目路径
        ANDROID_PROJECT_PATH = 'C:\\MyGit\\AutoBuildUnity_Build'
        //UnityLog Path
        UNITY_LOG_PATH = "C:\\IIS_ServerData\\${PROJECT_NAME}\\UnityLog\\V${VERSION_CODE}\\"
        //包体输出目录
        BUILD_OUTPUT_PATH = "C:\\IIS_ServerData\\${PROJECT_NAME}\\BuildOutput\\V${VERSION_CODE}\\"
    }
    stages {
        stage('Log And Init') {
            steps {
                script {
                    bat '''
                        @echo %PROJECT_NAME%
                        @echo %UNITY_EDITOR_PATH%
                        @echo %UNITY_PROJECT_PATH%
                        @echo %ANDROID_PROJECT_PATH%
                        @echo %UNITY_LOG_PATH%
                        @echo %BUILD_OUTPUT_PATH%
                    '''

                    // 获取当前时间并格式化
                    def currentTime = new Date().format('yyyy_MM_dd_HH_mm_ss')
                    echo "当前时间: ${currentTime}"

                    // 将当前时间传递给后续的 bat 脚本
                    env.CURRENT_TIME = currentTime
                }
            }
        }
        stage('Unity Git Sync') {
            when { expression { SYNC_UNITY_GIT == 'true' } }
            steps {
                timeout(time: 1, unit: 'MINUTES') {
                    script {
                        bat 'cd %UNITY_PROJECT_PATH% && git checkout -- .'
                        bat 'cd %UNITY_PROJECT_PATH% && git pull'
                    }
                }
            }
        }
        stage('Kill Unity') {
            when { expression { BUILD_UNITY == 'true' } }
            steps {
                script {
                    bat '''
                        REM 判断Unity是否运行中
                        TASKLIST /V /S localhost /U %username% > tmp_process_list.txt
                        TYPE tmp_process_list.txt | FIND "Unity.exe"

                        IF ERRORLEVEL 0 (
                            GOTO UNITY_IS_RUNNING
                        ) ELSE (
                            GOTO START_UNITY
                        )

                        :UNITY_IS_RUNNING
                        :: 杀掉Unity
                        TASKKILL /F /IM Unity.exe

                        :: 停1秒
                        PING 127.0.0.1 -n 3 >NUL

                        GOTO END

                        :END
                        REM 删除临时文件
                        del tmp_process_list.txt
                    '''
                }
            }
        }
        stage('Build Unity') {
            when { expression { BUILD_UNITY == 'true' } }
            steps {
                timeout(time: 60, unit: 'MINUTES') {
                    bat '''
                   "%UNITY_EDITOR_PATH%" -keep -batchmode -projectPath %UNITY_PROJECT_PATH% -executeMethod BuildProject.TestBuildSuccess -logFile %UNITY_LOG_PATH%%CURRENT_TIME%.log --productName:%PROJECT_NAME% --version:%VERSION_CODE% -buildTarget:Android -customParam:%UNITY_CUSTOME_PARAM%
                   '''
                }
            }
        }
        stage('Android Git Sync') {
            when { expression { SYNC_ANDDROID_GIT == 'true' } }
            steps {
                timeout(time: 30, unit: 'MINUTES') {
                    script {
                        bat 'cd %ANDROID_PROJECT_PATH% && git checkout -- .'
                        bat 'cd %UNITY_PROJECT_PATH% && git pull'
                    }
                }
            }
        }
        stage('Clean Android') {
            when { expression { CLEAN_ANDROID_CACHED == 'true' } }
            steps {
                timeout(time: 60, unit: 'MINUTES') {
                    bat '''
                    cd %ANDROID_PROJECT_PATH%
                    gradlew.bat clean
                   '''
                }
            }
        }
        stage('Sync Android') {
            steps {
                timeout(time: 60, unit: 'MINUTES') {
                    bat '''
                    cd %ANDROID_PROJECT_PATH%
                    gradlew syncReleaseLibJars --stacktrace
                   '''
                }
            }
        }
        stage('Build Release APK') {
            when { expression { BUILD_ANDROID_APK == 'true' } }
            steps {
                timeout(time: 60, unit: 'MINUTES') {
                    script {
                        bat '''
                            cd %ANDROID_PROJECT_PATH%
                            gradlew.bat assembleRelease -PcustomName=%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME% -PversionCode=%VERSION_CODE% -PversionName=%VERSION_NAME% --stacktrace
                            '''
                        bat '''
                            cd %ANDROID_PROJECT_PATH%
                            set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\apk\\release\\%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME%.apk"
                            apksigner sign --ks %pstoreFilefile%  --ks-pass pass:%storePassword%  --ks-key-alias %keyAlias% --key-pass pass:%keyPassword% --in %source% --out %source% 
                            '''
                        bat '''
                            @echo off
                            set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\apk\\release\\%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME%.apk"
                            set "dest=%BUILD_OUTPUT_PATH%%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME%.apk"

                            :: 创建目标目录（自动创建父目录）
                            if not exist "%dest%\\.." mkdir "%dest%\\.." 2>&1 || (
                                echo Failed to create target directory
                                exit /b 1
                            )

                            :: 检查源文件存在
                            if not exist "%source%" (
                                echo Source file not found: %source%
                                exit /b 2
                            )

                            :: 执行拷贝
                            copy /y "%source%" "%dest%" 2>&1 || (
                                echo Copy failed
                                exit /b 3
                            )
                        '''
                    }
                }
            }
        }
        stage('Build Debug APK') {
            when { expression { ONLY_RELEASE == 'false' && BUILD_ANDROID_APK == 'true' } }
            steps {
                timeout(time: 60, unit: 'MINUTES') {
                    script {
                         bat '''
                            cd %ANDROID_PROJECT_PATH%
                            gradlew.bat assembleDebug -ParchivesBaseName=%PROJECT_NAME%_%VERSION_CODE%_Debug _%CURRENT_TIME% -PversionCode=%VERSION_CODE% -PversionName=%VERSION_NAME% --stacktrace
                            '''
                        bat '''
                            cd %ANDROID_PROJECT_PATH%
                            set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\apk\\debug\\%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME%.apk"
                            apksigner sign --ks %pstoreFilefile%  --ks-pass pass:%storePassword%  --ks-key-alias %keyAlias% --key-pass pass:%keyPassword% --in %source% --out %source% 
                            '''
                        bat '''
                            @echo off
                            set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\apk\\debug\\%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME%.apk"
                            set "dest=%BUILD_OUTPUT_PATH%%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME%.apk"


                            :: 创建目标目录（自动创建父目录）
                            if not exist "%dest%\\.." mkdir "%dest%\\.." 2>&1 || (
                                echo Failed to create target directory
                                exit /b 1
                            )

                            :: 检查源文件存在
                            if not exist "%source%" (
                                echo Source file not found: %source%
                                exit /b 2
                            )

                            :: 执行拷贝
                            copy /y "%source%" "%dest%" 2>&1 || (
                                echo Copy failed
                                exit /b 3
                            )
                        '''
                    }
                }
            }
        }
        stage('Build Release AAB') {
            when { expression { BUILD_ANDROID_AAB == 'true' } }
            steps {
                timeout(time: 60, unit: 'MINUTES') {
                    script {
                        bat '''
                            cd %ANDROID_PROJECT_PATH%
                            gradlew.bat bundleRelease -PversionCode=%VERSION_CODE% -PversionName=%VERSION_NAME% --stacktrace
                            '''
                        // bat '''
                        //     cd %ANDROID_PROJECT_PATH%
                        //     apksigner sign --ks %pstoreFilefile%  --ks-pass pass:%storePassword%  --ks-key-alias %keyAlias% --key-pass pass:%keyPassword%
                        //     '''
                        bat '''
                            @echo off
                            set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\bundle\\release\\launcher-release.aab"
                            set "dest=%BUILD_OUTPUT_PATH%%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME%.aab"

                            :: 创建目标目录（自动创建父目录）
                            if not exist "%dest%\\.." mkdir "%dest%\\.." 2>&1 || (
                                echo Failed to create target directory
                                exit /b 1
                            )

                            :: 检查源文件存在
                            if not exist "%source%" (
                                echo Source file not found: %source%
                                exit /b 2
                            )

                            :: 执行拷贝
                            copy /y "%source%" "%dest%" 2>&1 || (
                                echo Copy failed
                                exit /b 3
                            )
                        '''
                    }
                }
            }
        }
        stage('Build Debug AAB') {
            when { expression { ONLY_RELEASE == 'false' && BUILD_ANDROID_AAB == 'true' } }
            steps {
                timeout(time: 60, unit: 'MINUTES') {
                    script {
                        bat '''
                            cd %ANDROID_PROJECT_PATH%
                            gradlew.bat bundleDebug -PversionCode=%VERSION_CODE% -PversionName=%VERSION_NAME% --stacktrace
                            '''
                        // bat '''
                        //     cd %ANDROID_PROJECT_PATH%
                        //     apksigner sign --ks %pstoreFilefile%  --ks-pass pass:%storePassword%  --ks-key-alias %keyAlias% --key-pass pass:%keyPassword%
                        //     '''
                        bat '''
                            @echo off
                            set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\bundle\\debug\\launcher-debug.aab"
                            set "dest=%BUILD_OUTPUT_PATH%%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME%.aab"

                            :: 创建目标目录（自动创建父目录）
                            if not exist "%dest%\\.." mkdir "%dest%\\.." 2>&1 || (
                                echo Failed to create target directory
                                exit /b 1
                            )

                            :: 检查源文件存在
                            if not exist "%source%" (
                                echo Source file not found: %source%
                                exit /b 2
                            )

                            :: 执行拷贝
                            copy /y "%source%" "%dest%" 2>&1 || (
                                echo Copy failed
                                exit /b 3
                            )
                        '''
                    }
                }
            }
        }
    }
}

