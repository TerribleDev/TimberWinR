echo "Running Deployment"
set
dir
mkdir TimberWinR.Builds\%APPVEYOR_BUILD_VERSION%
copy "TimberWix/bin/%Configuration%/TimberWinR-%APPVEYOR_BUILD_VERSION%.0.msi" "TimberWinR.Builds\%APPVEYOR_BUILD_VERSION%"
dir /s "TimberWinR.Builds"
git add TimberWinR.Builds.%APPVEYOR_BUILD_VERSION%/TimberWinR-%APPVEYOR_BUILD_VERSION%.0.msi
git commit -m"Added %APPVEYOR_BUILD_VERSION% MSI"
