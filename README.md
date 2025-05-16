# FluentLauncher.Extension.ConnectX
基于 [ConnectX](https://github.com/Corona-Studio/ConnectX/blob/main/README_CN.md) 开发的适用于 Fluent Launcher 的插件，提供用户友好的 UI 界面以便方便进行 Minecraft 远程联机

> [!IMPORTANT] 
> _**实验性功能**_  
> _目前仅在 Fluent Launcher 预览通道的 **部分** 发行版允许加载插件，并确定商店版本 **始终不会** 支持插件加载功能_  

![QQ_1747386453411](https://github.com/user-attachments/assets/38e6f2e1-0bab-46e8-bf7e-8a614d3cab87)

## 功能
在 Fluent Launcher 中窗口的侧栏添加了 `多人游戏` 的选项，内容就是为 ConnectX 本身的功能提供 UI 界面

### 网络
- [x] 支持用户自定义 ConnectX.Server 服务地址
- [x] 支持 P2P 打洞联机
- [x] 支持使用中继服务联机

### 房间管理
- [x] 通过邀请码分享房间
- [x] 支持创建带有密码的私人房间
- [x] 支持实时显示房间成员信息
- [x] 支持房主管理房间成员

## 安装

### 自动安装
- 下载仓库 [FluentLauncher.Extension.ConnectX](https://github.com/Xcube-Studio/FluentLauncher.Extension.ConnectX) 的 [Release](https://github.com/Xcube-Studio/FluentLauncher.Preview.Installer/releases) 中的 `FluentLauncher.UniversalInstaller` 安装向导来快速安装本插件到启动器

### 手动安装
- 下载本仓库 [Release](https://github.com/Xcube-Studio/FluentLauncher.Extension.ConnectX/releases/latest) 插件压缩包，然后在预览版本的 Fluent Launcher 中，打开 `设置` > `Extensions` > `Extensions storage directory` 来打开插件文件夹，将插件包解压到该目录下，然后重新运行启动器即可

## 服务
目前，我们在插件中提供了一个我们部署好的服务节点，这是完全免费的公益节点，如果遭遇攻击，我们会考虑终止该节点  

此外，在代码文件中 `ClientSettingProvider.cs` 我们不会列出我们的服务器地址，编译时请注意  

``` CSharp
private const string BaseServerAddress = "";
private const string MiaoVpsServerAddress = "";
```
