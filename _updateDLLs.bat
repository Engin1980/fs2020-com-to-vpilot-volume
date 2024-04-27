set "local=%cd%"

cd ..\ESystem.NET
call .\_updateRelease.bat

cd ..\ESimConnect
call .\_updateRelease.bat

cd %local%
copy ..\ESystem.NET\_Release\* .\DLLs\
copy ..\ESimConnect\_Release\* .\DLLs\

pause