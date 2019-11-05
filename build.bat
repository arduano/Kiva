call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat" 
del /S /F /Q build\*
rmdir /s /q build
mkdir build

del /S /F /Q Kiva-MIDI\bin\x64\Release\*
rmdir /s /q Kiva-MIDI\bin\x64\Release

MSBuild.exe Kiva.sln /t:Kiva-MIDI /p:Configuration=Release /p:Platform=x64
del /S Kiva-MIDI\bin\x64\Release\lib\*.xml 
del /S Kiva-MIDI\bin\x64\Release\lib\*.pdb
MSBuild.exe Kiva.sln /t:KivaInstaller /p:Configuration=Release /p:Platform=x64

powershell -c Compress-Archive -Path Kiva-MIDI\bin\x64\Release\* -CompressionLevel Optimal -Force -DestinationPath build\KivaPortable.zip
copy KivaInstaller\bin\Release\KivaInstaller.exe build\KivaInstaller.exe

pause