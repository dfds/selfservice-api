#!/usr/bin/env bash

macospath="/usr/local/share/dotnet"
linuxpath="/home/user/share/dotnet/dotnet"
windowspath="C:/program files/dotnet/"

# install dotnet csharpier if not installed
if [  ! "dotnet tool update csharpier -g" ]; then
    echo "Installing dotnet csharpier"
    dotnet tool install --global csharpier
fi

if [ -f ".git/hooks/pre-commit" ]; then
  
  timestamp=$(date +"%Y-%m-%d_%H-%M-%S")
  new_file="pre-commit-${timestamp}.old"

  echo "Found existing pre-commit hook, renaming old file to ${new_file}"
  mv .git/hooks/pre-commit .git/hooks/"${new_file}"
fi

echo "Creating pre-commit hook"
mkdir .git/hooks 2>&-
echo "#!/usr/bin/env bash" > .git/hooks/pre-commit
echo "export PATH=\"$PATH:$macospath:$linuxpath:$windowspath\"" >> .git/hooks/pre-commit

echo "dotnet csharpier ." >> .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
echo "Done"

