# DevKitChatBot

## 开始
1. 根据 [Get Started](https://microsoft.github.io/azure-iot-developer-kit/docs/get-started/) 搭建开发环境
1. 安装 [Visual Studio 2017] (https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio)
1. `git clone https://github.com/DevKitExamples/DevKitChatBot`
1. `cd DevKitChatBot`
1. `Code .`

## 创建WebSocket服务
1. 进入Web App本地源代码目录
```
cd DemoBotApp
start .
```

2. 双击 DemoBotApp.sln 用VS 2017打开解决方案
3. 按下F6键执行编译 （或选择Build -> Build solution）
4. 选择项目文件右键点击Publish进行发布

    ![Publish Web App](images/publish-1.jpg)

5. 选择创建一个新的Web服务实例

    ![Publish Web App](images/publish-2.jpg)

6. 配置Web服务实例，包含服务名，选择的Azure Subscription, Resource Group, App Service Plan等。然后点击**Create**按钮部署Web服务

    ![Publish Web App](images/publish-3.jpg)

7. 打开浏览器并输入：[https://portal.azure.cn](https://portal.azure.cn)

8. 打开Application settings, 开启Web sockets协议支持

    ![Publish Web App](images/publish-4.jpg)
9. 打开Application settings, 将以下三个配置项填写的

    CognitiveSubscriptionKey: XXX
    BotId: XXX
    DirectLineSecret: XXX

    ![Publish Web App](images/publish-5.jpg)

10. 重启Web服务使配置生效

## 更新DevKit SDK源代码

1. `git clone https://github.com/Microsoft/devkit-sdk`
2. `git checkout master_audio_v2`
3. 打开你的DevKit Arduino安装目录，C:\Users\{username}\AppData\Local\Arduino15\packages\AZ3166\hardware\stm32f4\1.2.0, 删除该目录下的所有文件
4. `cd devkit-sdk\AZ3166\src`
5. `start .`
6. 复制所有文件到步骤3的Arduino安装目录

## 将Arduino Sketch上传到DevKit

1. 将部署好的Web服务名称替换到*DevKitChatBot.ino*中 **[your web app name]**
   
   `static char * webAppUrl = "ws://[your web app name].azurewebsites.net";`

1. 将DevKit连接到电脑上
1. 点击Visual Stuio Code中的**任务**目录 - **运行Build任务…**
1. 等待Arduino Code上传

## 运行对话机器人
1. 按下按钮A开启对话模式
2. 按住按钮B开始说话，说完松开按钮B
3. 等待语音处理

测试对话集：
1. Hello / Good morning / How do you do
2. Could you introduce yourself?
3. Do you know Microsoft?
4. Who is Bill Gates?
5. How is the weather in Shanghai? / What's the weather like in Paris? / Tell me the weather in Los Angles.
Come some music please. / Could you play some music