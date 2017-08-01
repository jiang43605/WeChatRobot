![](https://img.shields.io/vso/build/chengf/b9824529-b4f9-4719-88cb-bc010f5991ae/7.svg) 

## 微信机器人

> 用于自动回复，定时发送，对接AI机器人等，运行在window平台下的可视化程序，主要是自用，项目写的较为简陋，因此，可能在使用时可能有一些莫名的异常。。。如果你在使用且不走运的遇到bug，请自己修复。。。不过大体还是能运行正常的。

### 引用

模拟网页微信Http过程，参照了：<https://github.com/sherlockchou86/WeChat.NET>，在他的基础上略微修改了一下，独立成`WXLogin`项目

### 使用说明

直接运行：Bin\WxRobot.exe  
查看插件效果：在`PowerShell`下运行`Bin\WxRobot.exe plugin`

界面主要有4个功能区域

- 近期联系列表
- 所有好友列表
- 规则列表
- 输出日记窗口

规则列表中有的是可以编辑的（按钮类型控件），单击即可出现编辑窗口。使用时，先选择一个规则然后设置好参数（如果该规则可以被编辑），再选择一个好友，点击最下面的`start`按钮即可将规则应用于该好友。

#### 扩展自己的规则

> **无接口限制，按约定编码即可**  

**常用的两个字段说明**  
userName：好友标识，可作为会话内唯一对好友的唯一标识符  
nickName：好友名称，即好友实际显示的名称  
msg:消息

扩展步骤：

1. 新建一个程序集，**建一个`Rule`类**，利用下表的数据接口编码，编译生成。
2. 将生成的`exe`或者`dll`放到主程序目录下的plugin目录下，如果有引用，将引用的程序集也放到该目录下，注意程序集的命名空间和名称最好不要和已有的重名。

接口：  

| 名称 | 类型 | 备注 |
| ---- | --- | ---- |
| RuleUser | `Dictionary<string, string>` | 静态只读字段，存储订阅该规则的用户信息, `Key`表示`userName`, `value`表示`nickName` |
| Name | `String` | 只读属性，表示在规则列表显示的名称 |
| Me | `Tuple<string, string>` | 属性，`item1`表示`nickName`，`item2`表示`userName` |
| SendMsg | `Action<string, string>` | 属性，用于发送消息，使用：`SendMsg(arg1,arg2)`，`arg1`表示要发送用户的`userName`，`arg2`表示要发送的消息 |
| Click | `void Click()` | 方法，当用户点击该规则时触发的操作 |
| MsgHandle | `string MsgHandle(string userName, string msg, int type)` | 方法，当该用户发消息过来，调用该方法，`type`表示消息类型，请上网查阅具体含义 |
| BingDing | `void BingDing(string userName)` | 方法，用户第一次应用该规则时触发 |
| FromMe | `string FromMe(string userName, string msg, int type)` | 方法，当用户自己从其它设备上回复消息时，触发该方法 |

**以上接口不强制要求写，不保证接口稳定，如果编写的扩展代码逻辑错误或抛出异常，可能导致主界面崩溃。。。排查时请考虑主程序代码有bug**