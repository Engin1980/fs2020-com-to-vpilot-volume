set "local=%cd%"

cd ..\..\ESystem.NET
call .\updateRelease.bat

cd %local%
copy ..\..\ESystem.NET\_Release\* .\

del updateRelease.bat

pause