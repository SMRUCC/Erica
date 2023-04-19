@echo off

REM git remote add local http://192.168.0.232:8848/xieguigang/Erica.git 

git pull gitlink HEAD
git push gitlink HEAD

git pull local HEAD
REM git pull gitee HEAD

git push local HEAD
REM git push gitee HEAD

echo synchronization of this code repository job done!