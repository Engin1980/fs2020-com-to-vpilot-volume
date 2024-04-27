set "local=%cd%"

cd ..\ESystem.NET
call .\_updateRelease.bat

cd %local%
copy ..\ESystem.NET\_Release\* .\DLLs\

pause