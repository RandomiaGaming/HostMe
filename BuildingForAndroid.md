Unfortunately we cannot easily install dotnet on android.
That means we must create a fully self contained build that is ahead of time compiled to native Arm64 assembly.
Unfortunately dotnet for windows cannot cross compile to native linux binaries.
Luckily we can use wsl to solve this problem using wsl.
First install wsl by running wsl --install from an administrator command prompt.
Then once inside wsl make sure dotnet is installed with:
sudo snap install dotnet-sdk --classic
Then make sure the D: drive is mounted with:
sudo mkdir /mnt/d
sudo mount -t drvfs D: /mnt/d
Then build with:
dotnet publish -r linux-bionic-arm64 -c Release -o ./HostMe4Android