dotnet build midilonia.iOS/midilonia.iOS.csproj -c Debug --self-contained -p:RuntimeIdentifier=ios-arm64 -p:_DeviceName=00008130-001A6C390221001C
ios-deploy --bundle midilonia.iOS/bin/Debug/net8.0-ios/ios-arm64/midilonia.iOS.app
