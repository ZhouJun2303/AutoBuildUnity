pipeline {
    agent any
    environment {
        //项目名称
        PROJECT_NAME = "PipelineTEST"
        //Unity 编辑器路径
        UNITY_EDITOR_PATH = "C:\\Program Files\\Unity\\Hub/Editor\\2021.3.20f1\\Editor\\Unity.exe"
        //Unity 项目路径
        UNITY_PROJECT_PATH = "C:/MyGit/AutoBuildUnity"
        //Android 项目路径
        ANDROID_PROJECT_PATH = "C:/MyGit/AutoBuildUnity_Build"
    }
    stages {
        stage('Log') {
            steps {
                bat '''
                    @echo %PROJECT_NAME% 
                    @echo %UNITY_EDITOR_PATH% 
                    @echo %UNITY_PROJECT_PATH% 
                    @echo %ANDROID_PROJECT_PATH% 
                    @echo %VERSION_NAME% 
                '''
            }
        }
        stage('Unity Git Sync') {
            steps {
                timeout(time: 1, unit: 'MINUTES') {
                    script {
                       bat 'cd %UNITY_PROJECT_PATH% && git checkout -- .'
                       bat 'cd %UNITY_PROJECT_PATH% && git pull'
                    }
                }
            }
        }
        stage("Kill Unity"){
             when{expression {BUILD_UNITY == "true"}}
             steps{
                 script{
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
        stage("Build Unity") {
           when { expression { BUILD_UNITY == "true" } }
           steps {
               bat '''
               "C:\\Unity\\UnityEditor\\Unity 2020.3.33f1\\Editor\\Unity.exe" -keep -batchmode -projectPath C:\\MyGit\\AutoBuildUnity -executeMethod BuildProject.TestBuildSuccess -logFile C:\\IIS_ServerData\\PipelineTEST\\UnityLog\\100.log --productName:Idle_Lose_Weight --version:1.0.0 -buildTarget:Android -customParam:1
               '''
           }
        }
        stage('Android Git Sync') {
            steps {
                timeout(time: 1, unit: 'MINUTES') {
                    script {
                       bat 'cd %ANDROID_PROJECT_PATH% && git checkout -- .'
                       bat 'cd %UNITY_PROJECT_PATH% && git pull'
                    }
                }
            }
        }   
    }
}
