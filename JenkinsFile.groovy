pipeline {
    agent any
    environment {
        PROJECT_NAME = 'PipelineTEST'
        UNITY_EDITOR_PATH = "D:\\Unity\\Unity 2021.3.45f1\\Editor\\Unity.exe"
        UNITY_PROJECT_PATH = 'D:\\MyGit\\AutoBuildUnity'
        ANDROID_PROJECT_PATH = 'D:\\MyGit\\AutoBuildUnity_Build'
        UNITY_LOG_PATH = "C:\\IIS_ServerData\\${PROJECT_NAME}\\UnityLog\\V${VERSION_CODE}\\"
        BUILD_OUTPUT_PATH = "C:\\IIS_ServerData\\${PROJECT_NAME}\\BuildOutput\\V${VERSION_CODE}\\"
        // 修改为你本机的 apksigner.bat 路径
        APKSIGNER = "D:\\SDK\\build-tools\\36.0.0\\apksigner.bat"
    }
    stages {
        stage('Check Environment') {
            steps {
                script {
                    bat '''
                        echo ===== 检查环境依赖 =====

                        if not exist "%APKSIGNER%" (
                            echo ERROR: apksigner 不存在: %APKSIGNER%
                            exit /b 1
                        )
                        if not exist "%UNITY_EDITOR_PATH%" (
                            echo ERROR: Unity Editor 不存在: %UNITY_EDITOR_PATH%
                            exit /b 2
                        )
                        if not exist "%UNITY_PROJECT_PATH%" (
                            echo ERROR: Unity Project 路径不存在: %UNITY_PROJECT_PATH%
                            exit /b 3
                        )
                        if not exist "%ANDROID_PROJECT_PATH%" (
                            echo ERROR: Android Project 路径不存在: %ANDROID_PROJECT_PATH%
                            exit /b 4
                        )

                        echo ===== 检查完成，环境正常 =====
                    '''
                }
            }
        }

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
                        @echo %APKSIGNER%
                    '''
                    def currentTime = new Date().format('yyyy_MM_dd_HH_mm_ss')
                    echo "当前时间: ${currentTime}"
                    env.CURRENT_TIME = currentTime
                }
            }
        }


        stage('Unity Git Sync') {
            when { expression { SYNC_UNITY_GIT == 'true' } }
            steps {
                timeout(time: 1, unit: 'MINUTES') {
                    dir("${env.UNITY_PROJECT_PATH}") {
                        bat 'git checkout -- .'
                        bat 'git pull'
                    }
                }
            }
        }

        stage('Kill Unity') {
            when { expression { BUILD_UNITY == 'true' } }
            steps {
                bat '''
                    TASKLIST /V /S localhost /U %username% > tmp_process_list.txt
                    TYPE tmp_process_list.txt | FIND "Unity.exe"
                    IF ERRORLEVEL 0 (
                        TASKKILL /F /IM Unity.exe
                        PING 127.0.0.1 -n 3 >NUL
                    )
                    del tmp_process_list.txt
                '''
            }
        }

        stage('Build Unity') {
            when { expression { BUILD_UNITY == 'true' } }
            steps {
                timeout(time: 60, unit: 'MINUTES') {
                    bat "\"%UNITY_EDITOR_PATH%\" -keep -batchmode -projectPath %UNITY_PROJECT_PATH% -executeMethod BuildProject.TestBuildSuccess -logFile %UNITY_LOG_PATH%%CURRENT_TIME%.log --productName:%PROJECT_NAME% --version:%VERSION_CODE% -buildTarget:Android -customParam:%UNITY_CUSTOME_PARAM%"
                }
            }
        }

        stage('Android Git Sync') {
            when { expression { SYNC_ANDDROID_GIT == 'true' } }
            steps {
                timeout(time: 30, unit: 'MINUTES') {
                    dir("${env.ANDROID_PROJECT_PATH}") {
                        bat 'git checkout -- .'
                        bat 'git pull'
                    }
                }
            }
        }

        stage('Clean Android') {
            when { expression { CLEAN_ANDROID_CACHED == 'true' } }
            steps {
                timeout(time: 60, unit: 'MINUTES') {
                    dir("${env.ANDROID_PROJECT_PATH}") {
                        bat 'gradlew.bat clean'
                    }
                }
            }
        }

        stage('Sync Android') {
            steps {
                timeout(time: 60, unit: 'MINUTES') {
                    dir("${env.ANDROID_PROJECT_PATH}") {
                        bat 'gradlew syncReleaseLibJars --stacktrace'
                    }
                }
            }
        }

       stage('Build Release APK') {
    when { expression { BUILD_ANDROID_APK == 'true' } }
    steps {
        timeout(time: 60, unit: 'MINUTES') {
            dir("${env.ANDROID_PROJECT_PATH}") {
                bat '''
                    gradlew.bat assembleRelease ^
                        -PcustomName=%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME% ^
                        -PversionCode=%VERSION_CODE% ^
                        -PversionName=%VERSION_NAME% ^
                        --stacktrace

                    set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\apk\\release\\%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME%.apk"
                    if not exist "%source%" ( echo ERROR: APK 文件不存在 & exit /b 3 )

                    call "%APKSIGNER%" sign ^
                        --ks "%pstoreFilefile%" ^
                        --ks-pass pass:%storePassword% ^
                        --ks-key-alias %keyAlias% ^
                        --key-pass pass:%keyPassword% ^
                        --in "%source%" ^
                        --out "%source%"
                '''
                bat '''
                    set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\apk\\release\\%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME%.apk"
                    set "dest=%BUILD_OUTPUT_PATH%%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME%.apk"
                    if not exist "%dest%\\.." mkdir "%dest%\\.."
                    copy /y "%source%" "%dest%"
                '''
            }
        }
    }
}

stage('Build Debug APK') {
    when { expression { ONLY_RELEASE == 'false' && BUILD_ANDROID_APK == 'true' } }
    steps {
        timeout(time: 60, unit: 'MINUTES') {
            dir("${env.ANDROID_PROJECT_PATH}") {
                bat '''
                    gradlew.bat assembleDebug ^
                        -PcustomName=%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME% ^
                        -PversionCode=%VERSION_CODE% ^
                        -PversionName=%VERSION_NAME% ^
                        --stacktrace

                    set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\apk\\debug\\%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME%.apk"
                    if not exist "%source%" ( echo ERROR: APK 文件不存在 & exit /b 3 )

                    call "%APKSIGNER%" sign ^
                        --ks "%pstoreFilefile%" ^
                        --ks-pass pass:%storePassword% ^
                        --ks-key-alias %keyAlias% ^
                        --key-pass pass:%keyPassword% ^
                        --in "%source%" ^
                        --out "%source%"
                '''
                bat '''
                    set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\apk\\debug\\%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME%.apk"
                    set "dest=%BUILD_OUTPUT_PATH%%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME%.apk"
                    if not exist "%dest%\\.." mkdir "%dest%\\.."
                    copy /y "%source%" "%dest%"
                '''
            }
        }
    }
}

stage('Build Release AAB') {
    when { expression { BUILD_ANDROID_AAB == 'true' } }
    steps {
        timeout(time: 60, unit: 'MINUTES') {
            dir("${env.ANDROID_PROJECT_PATH}") {
                bat '''
                    gradlew.bat bundleRelease ^
                        -PcustomName=%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME% ^
                        -PversionCode=%VERSION_CODE% ^
                        -PversionName=%VERSION_NAME% ^
                        --stacktrace

                    set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\bundle\\release\\%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME%.aab"
                    if not exist "%source%" ( echo ERROR: AAB 文件不存在 & exit /b 3 )

                    call "%APKSIGNER%" sign ^
                        --ks "%pstoreFilefile%" ^
                        --ks-pass pass:%storePassword% ^
                        --ks-key-alias %keyAlias% ^
                        --key-pass pass:%keyPassword% ^
                        --in "%source%" ^
                        --out "%source%"
                '''
                bat '''
                    set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\bundle\\release\\%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME%.aab"
                    set "dest=%BUILD_OUTPUT_PATH%%PROJECT_NAME%_%VERSION_CODE%_Release_%CURRENT_TIME%.aab"
                    if not exist "%dest%\\.." mkdir "%dest%\\.."
                    copy /y "%source%" "%dest%"
                '''
            }
        }
    }
}

stage('Build Debug AAB') {
    when { expression { ONLY_RELEASE == 'false' && BUILD_ANDROID_AAB == 'true' } }
    steps {
        timeout(time: 60, unit: 'MINUTES') {
            dir("${env.ANDROID_PROJECT_PATH}") {
                bat '''
                    gradlew.bat bundleDebug ^
                        -PcustomName=%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME% ^
                        -PversionCode=%VERSION_CODE% ^
                        -PversionName=%VERSION_NAME% ^
                        --stacktrace

                    set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\bundle\\debug\\%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME%.aab"
                    if not exist "%source%" ( echo ERROR: AAB 文件不存在 & exit /b 3 )

                    call "%APKSIGNER%" sign ^
                        --ks "%pstoreFilefile%" ^
                        --ks-pass pass:%storePassword% ^
                        --ks-key-alias %keyAlias% ^
                        --key-pass pass:%keyPassword% ^
                        --in "%source%" ^
                        --out "%source%"
                '''
                bat '''
                    set "source=%ANDROID_PROJECT_PATH%\\launcher\\build\\outputs\\bundle\\debug\\%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME%.aab"
                    set "dest=%BUILD_OUTPUT_PATH%%PROJECT_NAME%_%VERSION_CODE%_Debug_%CURRENT_TIME%.aab"
                    if not exist "%dest%\\.." mkdir "%dest%\\.."
                    copy /y "%source%" "%dest%"
                '''
            }
        }
    }
}

    }
}
