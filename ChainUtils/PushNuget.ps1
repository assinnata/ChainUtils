del *.nupkg
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\ChainUtils\ChainUtils.csproj" -p:Configuration=Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\ChainUtils.Portable\ChainUtils.Portable(Profile111).csproj" -p:Configuration=Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\ChainUtils.Portable\ChainUtils.Portable(Profile259).csproj" -p:Configuration=Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\ChainUtils.Portable\ChainUtils(MonoAndroid).csproj" -p:Configuration=Release
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Build\Deploy.csproj"

nuGet pack ChainUtils.nuspec

forfiles /m *.nupkg /c "cmd /c NuGet.exe push @FILE"
(((dir *.nupkg).Name) -match "[0-9]+?\.[0-9]+?\.[0-9]+?\.[0-9]+")
$ver = $Matches.Item(0)
git tag -a "v$ver" -m "$ver"
git push --tags