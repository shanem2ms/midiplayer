dotnet build midilonia.iOS/midilonia.iOS.csproj --self-contained -p:RuntimeIdentifier=ios-arm64 -p:_DeviceName=00008130-001A6C390221001C
#codesign --deep --force --verbose --sign "Apple Development: Shane Morrison (EE6F6TUKRZ)" midilonia.iOS/bin/Debug/net7.0-ios/ios-arm64/midilonia.iOS.app

#check signature
#codesign -dv --verbose=4 midilonia.iOS/bin/Debug/net7.0-ios/ios-arm64/midilonia.iOS.app
#/Users/shanemorrison/Library/Developer/Xcode/DerivedData/mytestapp-csszqfjuoifijnbtwbdyldpvkkin/Build/Products/Debug-iphoneos/mytestapp.app
ios-deploy --bundle midilonia.iOS/bin/Debug/net7.0-ios/ios-arm64/midilonia.iOS.app
