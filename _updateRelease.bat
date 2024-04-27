deltree -Y _Release
mkdir _Release
mkdir .\_Release\FS2020ComToVPilotVolume
copy readme.md .\_Release\FS2020ComToVPilotVolume\_Readme.md
copy License .\_Release\FS2020ComToVPilotVolume\_License.txt
xcopy /e /i /Y com2vpilotvolume\bin\debug\net6.0-windows\* .\_Release\FS2020ComToVPilotVolume
cd .\_Release
tar.exe -c -f FS2020ComToVPilotVolume.zip FS2020ComToVPilotVolume



pause
