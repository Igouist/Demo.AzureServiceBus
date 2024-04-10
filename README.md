# 使用 Azure Service Bus 來建立簡單的訊息佇列（Message Queue）吧
Azure Service Bus 簡單筆記的範例專案

> 本文同步發表於部落格（好讀版 →）：https://igouist.github.io/post/2022/08/azure-service-bus/

![Image](https://i.imgur.com/5Vube9E.png)

在工作上遇到在兩個 Azure 工具間建立訊息佇列（Message Queue）的需求，因此接觸到了 Azure Service Bus（中文：服務匯流排 ~~燴牛排？~~），在前輩的協助下建立了一組簡單的 Demo，這就來筆記一下。

## 什麼是訊息佇列（Message Queue, MQ）

首先讓我們簡單認識一下訊息佇列。假設我們有生產者和消費者兩個服務，其中**生產者負責產生資料，而消費者負責消費這些資料**：

![Image](https://i.imgur.com/mg4lXJk.png)

各位也可以這樣理解：生產者就像是壽司師傅，他會不斷的捏壽司出來；而這時候來了一位大胃王顧客，他就是消費者，會不斷地把壽司吃掉

![Image](https://i.imgur.com/Sk16WdC.png)

大概對這兩個角色有點認識就行了。那麼，假設我們有兩組 API 服務：其中一個是負責寫入 Log 的服務，而另一個是產品服務。

產品服務會將 Log 內容丟給 Log 服務去紀錄 Log，這時候產生了這些日誌資料的產品服務就是生產者，而消費這些日誌資料去寫 Log 的服務就是消費者。

也就是：**`產品服務（生產者） —— 資料 —> Log 服務（消費者）`** 這樣的狀況。

然而像這樣**直接相依的兩個服務，可能就會遇到一些問題：像是消費者突然掛掉，導致生產者也跟著掛掉；又或是消費者的變動和擴展會連帶影響到生產者必須跟著變動等等。**

<!--more-->

這樣說可能有些模糊。就以上面提到的 Log 服務來說，在直接呼叫 API 的情況下，我們常常會需要在產品服務中直接明確寫出 Log 服務的資訊，像是站台位址、API 路由之類的。

但當 Log 服務的站台或方法有變動，我們就被迫要修改產品服務對應的程式碼；而如果我們今天想要擴展 Log 服務的站台，就會需要修改產品服務裡關於 Log 服務的資訊。

而今天如果 Log 服務短暫掛掉了，可能就會連帶讓我們的產品服務一起掛掉，就算有進行簡單的錯誤處理，當時該記的 Log 內容也就遺失了。

那面對這些問題的時候怎麼辦呢？這時候我們就可以**在中間加一條佇列**：

![Image](https://i.imgur.com/zrgm8JC.png)

用前面的壽司店來說就是這樣（很堅持要用壽司舉例）：

![Image](https://i.imgur.com/J6aksgo.png)

我們加入訊息佇列之後得到的好處有：

- **解除耦合**
  - 生產者不需要知道消費者的資訊，消費者的變動也不會直接影響到生產者
- **提高擴展性**
  - 即使我們要增加消費者的數量，變成三個消費者來處理這些資料，也只要讓佇列處理轉發就好，而不用影響生產者
- **非同步**
  - 生產者現在只要把訊息丟到佇列就可以回頭繼續做自己的事了，不用管也不用等待消費者處理這些訊息
- **緩衝**
  - 佇列讓消費者多了一段緩衝區，即使消費者忙不過來，也有佇列可以讓訊息好好排隊

除此之外還可以玩一些花式處理，例如藉由佇列來做到限制流量等等，此處先按下不表。

現在我們大概知道訊息佇列在幹嘛了。接著就讓我們來玩玩 Azure Service Bus 這個訊息佇列服務吧！

> 想更了解 Message Queue 的朋友，也可以閱讀以下的參考資料呦：
> 
> - [什么是消息队列？ - Java3y (qq.com)](https://mp.weixin.qq.com/s?__biz=MzI4Njg5MDA5NA==&mid=2247485080&idx=1&sn=f223feb9256727bde4387d918519766b)
> - [消息队列（Message Queue）基本概念 | NingG 个人博客](http://ningg.top/message-queue-intro/)
> - [Message Queue - (1) - iT 邦幫忙 (ithome.com.tw)](https://ithelp.ithome.com.tw/articles/10238631)
> - [Producer Consumer 模式 - iT 邦幫忙 (ithome.com.tw)](https://ithelp.ithome.com.tw/articles/10209296)
> - [什麼是訊息佇列？ (amazon.com)](https://aws.amazon.com/tw/message-queue/)

## 建立 Azure Service Bus 資源

> 小提示：Azure Service Bus 是一項收費服務，你可能會想先了解[定價](https://azure.microsoft.com/zh-tw/pricing/details/service-bus/)

首先讓我們先到 [Azure](https://portal.azure.com/#home)，建立一個新的資源：

![Image](https://i.imgur.com/s0klgGc.png)

找到 Service Bus 並新建它：

![Image](https://i.imgur.com/ngSiiJm.png)

![Image](https://i.imgur.com/2TepYi8.png)

接著選擇要掛資源的帳戶和群組後，替我們的 Service Bus 取個好記的名字。

這邊示範的方案當然就直接選最便宜的 XD

![Image](https://i.imgur.com/6N744Nr.png)

除了基本設定以外，這邊還能變更最低 TLS 版本（維護老舊專案者注意，或是準備[踩雷](https://blog.darkthread.net/blog/net35-tls12-issue/)）等等，可以頁籤都戳一戳。

確認之後就可以勇敢按下「審核 + 建立」囉！

按下後會部署個幾分鐘，部屬完畢之後就可以直接前往資源：

![Image](https://i.imgur.com/oPmqwpi.png)

抵達我們的 Service Bus 資源頁面：

![Image](https://i.imgur.com/CvH4MgL.png)

這樣我們就成功建立 Service Bus 啦！

## 建立佇列（Queue）

接著就讓我們來建立一條佇列吧，首先讓我們再看一眼微軟把拔提供的佇列概念圖：

![Image](https://i.imgur.com/Oi1GOqz.png)

回到 Azure 的 Service Bus 資源頁面，讓我們在畫面上點選建立佇列：

![Image](https://i.imgur.com/lwNmNQR.png)

並且取個好名字，等等發訊息的時候會用到：

![Image](https://i.imgur.com/6ONWYWT.png)

> 這邊會需要注意一下「最大傳遞計數」（Maximum Delivery Count），簡單來說就是這封訊息會嘗試傳遞幾次，如果超過次數都沒有傳遞成功就會被丟到無效信件。可以參閱[服務匯流排寄不出的信件佇列的概觀](https://docs.microsoft.com/zh-tw/azure/service-bus-messaging/service-bus-dead-letter-queues#maximum-delivery-count)中的「最大傳遞計數」小節

> 這篇我們不會用到下面選項的[資料分割](https://docs.microsoft.com/zh-tw/azure/service-bus-messaging/service-bus-partitioning)、到期自動轉[無效信件](https://docs.microsoft.com/zh-tw/azure/service-bus-messaging/service-bus-dead-letter-queues)等功能，有興趣的朋友可以再自己研究一下呦～

建立完就會出現在我們 Service Bus 的頁面下方囉：

![Image](https://i.imgur.com/Jbzbei8.png)

## 取得 ServiceBus 的連線字串

接著為了讓我們後續可以順利連線，先到左邊的 **「設定 > 共用存取原則」拿到我們的連線字串**：

![Image](https://i.imgur.com/zYkN3cQ.png)

這邊直接使用預設的原則，如果有需要控管不同使用者的權限，例如說某組連線字串只能接收或讀取等等，也可以在這邊新增原則來管理。

## 將訊息放入佇列

現在 Queue 已經建立起來了，接著就是要嘗試把訊息丟到 Queue 裡面囉！

為了方便測試，這邊就採用 Visual Studio 內建的 API 樣板，直接在 Controller 簡單建立一個範例，要實際應用在專案中的朋友請再依據專案架構自行調整。

那麼就讓我們開始吧，首先我們會需要安裝 **Azure.Messaging.ServiceBus** 這個套件包：

![Image](https://i.imgur.com/zXuFLdw.png)

接著是我們本次的範例用 Controller，以及尚未實作傳送訊息的 Function：

```csharp
/// <summary>
/// Service Bus Queue 示範用 Controller
/// </summary>
[ApiController]
[Route("[controller]")]
public class QueueController : ControllerBase
{
    /// <summary>
    /// 將訊息放入佇列
    /// </summary>
    [HttpPost]
    public async Task Enqueue([FromBody] string context)
    {
        // 我們要在這裡實作傳送訊息到 Queue 的方法
    }
}
```

### 傳送一則訊息到佇列

接著讓我們來傳送訊息吧，這邊會需要：

- 用 ServiceBus 的連線字串建立 `ServiceBusClient`
- 用 Queue 的名稱，從 `ServiceBusClient` 建立 `ServiceBusSender`
- 用 `ServiceBusSender` 來傳送訊息到 Queue

```csharp
/// <summary>
/// 將訊息放入佇列
/// </summary>
[HttpPost]
public async Task Enqueue([FromBody] string context)
{
    // 用 ServiceBus 的連線字串建立 Client
    // 連線字串可以在 Azure ServiceBus 頁面的共用存取原則找到
    // ServiceBusClient 用完記得要呼叫 DisposeAsync() 來關掉
    // 或是直接使用 await using 包起來
    var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
    await using var client = new ServiceBusClient(connectionString);

    // 傳遞 Queue 的名字給 CreateSender 方法來建立 Sender
    // 和 ServiceBusClient 一樣，有提供 DisposeAsync 方法來關閉
    // 或是直接使用 await using 包起來
    var queueName = "YOUR QUEUE NAME";
    await using var sender = client.CreateSender(queueName);

    // 將要傳送的訊息包裝成 ServiceBusMessage
    // 並使用 ServiceBusSender.SendMessageAsync 傳送出去
    var message = new ServiceBusMessage(context);
    await sender.SendMessageAsync(message);
}
```
提醒一下將來會跑回來複製貼上的我和各位朋朋：記得把連線字串跟佇列名稱改成你的！

現在讓我們來呼叫ＡＰＩ，丟個 `"Hello"` 進去試試：

![Image](https://i.imgur.com/ioXGear.png)

接著到 Azure Service Bus 的 Queue 介面瞧瞧：

![Image](https://i.imgur.com/1zTEnWt.png)

Queue 也確實收到一則訊息了，看來我們成功把 Hello 丟進去啦！

### 傳送一批訊息到佇列

當然在實務上，我們有時候會想要傳一卡車的訊息；例如我們剛處理完一批客戶，想把它們丟到佇列去讓另一個服務做點事，這時候如果還得一封一封塞訊息就有點怪怪的。

雖然我們可以直接粗暴地把 Sender 的 `SendMessageAsync` 加個 `s` 變成 `SendMessagesAsync`，這樣它就能接收串列的 `IEnumerable<ServiceBusMessage>` 了（真貼心啊 Azure）

不過，我們還可以選擇使用 [ServiceBusMessageBatch](https://docs.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusmessagebatch) 這個工具來幫我們一次發他個一批訊息。

這邊也示範一下。~~讓我之後可以回來抄~~

```csharp
/// <summary>
/// 將一堆訊息放入佇列
/// </summary>
/// <param name="context"></param>
/// <returns></returns>
[HttpPost("Batch")]
public async Task EnqueueBatch([FromBody] string context)
{
    // 把訊息重複個十次，假裝我們有很多訊息
    var contexts = Enumerable.Repeat(context, 10);
    
    // 和單則訊息的場合一樣：先建立 Client 及 Sender
    var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
    await using var client = new ServiceBusClient(connectionString);
    
    var queueName = "YOUR QUEUE NAME";
    await using var sender = client.CreateSender(queueName);

    // 從 Sender 來建立一批訊息（類似郵差包的感覺）
    using var messageBatch = await sender.CreateMessageBatchAsync();

    // 將訊息逐一嘗試放到這批訊息中（把信塞到郵差包的感覺）
    foreach (var text in contexts)
    {
        var message = new ServiceBusMessage(text);
        if (messageBatch.TryAddMessage(message) is false)
        {
            throw new Exception("放入訊息失敗");
        }
    }

    // 把整個郵差包丟出去
    await sender.SendMessagesAsync(messageBatch);
}
```

直接多十條訊息，看起來沒問題：

![Image](https://i.imgur.com/iZxuUqu.png)

### 傳送物件到佇列

Ｑ：我要傳的東西不是 string 而是物件怎麼辦？

Ａ：山不轉路轉，就用 `JsonSerializer.Serialize` 轉成 Json 再傳。

好的結案。

## 從佇列取出訊息

現在我們已經可以把訊息丟到 Queue 中了，接下來當然就是要拿出來囉！

拿出來的時候也有幾個不同的姿勢，接著就讓我們一一介紹下：

### 從佇列中讀取一則訊息

就像我們要寫入訊息的時候，要從 `ServiceBusClient` 建立一個 `ServiceBusSender` 一樣，當我們要寄送訊息的時候，也要從 `ServiceBusClient` 建立一個 `ServiceBusReceiver` 來幫我們處理訊息：

```csharp
/// <summary>
/// 取出佇列中的單則訊息
/// </summary>
/// <returns></returns>
[HttpGet("Receive")]
public async Task<string> Receive()
{
    // 和發送訊息的場合差不多：先建立 Client 及 Receiver
    var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
    await using var client = new ServiceBusClient(connectionString);

    // 和前面的 ServiceBusSender 一樣，有提供 DisposeAsync 方法讓我們用完時關閉
    // 或是直接使用 await using 包起來
    var queueName = "YOUR QUEUE NAME";
    await using var receiver = client.CreateReceiver(queueName);

    // 使用 ReceiveMessageAsync 來把訊息讀取出來
    var message = await receiver.ReceiveMessageAsync();
    var body = message.Body.ToString();

    // 告訴 Service Bus 這個訊息有成功處理了
    await receiver.CompleteMessageAsync(message);
    return body;
}
```

過程相當的簡單，只需要叫 `ServiceBusReceiver` 幫忙拿出來就好。現在讓我們呼叫試試：

![Image](https://i.imgur.com/H9v2PdJ.png)

成功取得我們前面放進去的訊息囉！

### 從佇列中持續讀取訊息

前面提到的 `ServiceBusReceiver` 可以從佇列中取出一則訊息，但大多時候訊息的接收方是被動的，也就說接收方其實並不知道發送方傳訊息了沒、現在有沒有訊息，更不用說主動去取出訊息了。

因此通常的作法是採用被動接收訊息再進行處理的方式：**事先註冊好處理訊息的事件，當有訊息進來的時候就按照指示去進行處理**，這時候我們就會需要用到 `ServiceBusProcessor`。

並且當 `ServiceBusProcessor` 建立之後，我們會需要告訴他兩件事：我們想怎麼處理訊息、出錯的時候該怎麼辦。

這些都設定完之後，`ServiceBusProcessor` 就會上工站崗。只要有訊息進來，它就會按照我們給的小抄去執行

整理一下，我們接收 Service Queue 的訊息時會需要：

- 用 ServiceBus 的連線字串建立 `ServiceBusClient`
- 用 Queue 的名稱，從 `ServiceBusClient` 建立 `ServiceBusProcessor`
- 設定接收到訊息之後的處理方式 `ProcessMessageAsync`
- 設定出錯時的處理方式 `ProcessErrorAsync`
- 讓 `ServiceBusProcessor` 持續接收訊息

```csharp
/// <summary>
/// 取出佇列中的訊息
/// </summary>
/// <returns></returns>
public async Task Dequeue()
{
    // 和發送訊息的場合差不多：先建立 Client 及 Processor
    var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
    await using var client = new ServiceBusClient(connectionString);

    // 和前面的 ServiceBusSender 一樣，有提供 DisposeAsync 方法讓我們用完時關閉
    // 或是直接使用 await using 包起來
    var queueName = "YOUR QUEUE NAME";
    await using var processor = client.CreateProcessor(queueName);

    // 告訴 Processor 我們想怎麼處理訊息
    processor.ProcessMessageAsync += MessageHandler;
    processor.ProcessErrorAsync += ErrorHandler;

    // 讓 Processor 上工，開始接收訊息
    await processor.StartProcessingAsync();

    // 實際上會掛著讓 processor 一直處理送來的訊息
    // 這邊示範而已就意思意思跑個一下下
    await Task.Delay(1000);

    // 讓 Processor 下班休息
    await processor.StopProcessingAsync();
}

/// <summary>
/// 處理佇列訊息
/// </summary>
/// <returns></returns>
private static async Task MessageHandler(ProcessMessageEventArgs args)
{
    // 從訊息的 Body 取出我們發送時塞進去的內容
    var message = args.Message.Body.ToString();

    // 對訊息內容做你想做的事
    // 這邊就印出來看個一眼意思意思
    Console.WriteLine(message);

    // 告訴 Service Bus 這個訊息有成功處理了
    await args.CompleteMessageAsync(args.Message);
}

/// <summary>
/// 處理佇列錯誤訊息
/// </summary>
/// <returns></returns>
private static async Task ErrorHandler(ProcessErrorEventArgs args)
{
    // 從訊息中取出錯誤訊息$$
    var exception = args.Exception.ToString();

    // 對訊息做一些錯誤處理，例如存到日誌系統之類的
    // 這邊也印出來看個一眼意思意思
    Console.WriteLine(exception);
}
```

接著就讓我們呼叫看看：

![Image](https://i.imgur.com/hKoP1bD.png)

可以看見我們確實收到了前面發送的「Hello」，接收大成功！

### 補充：在 ServiceBus Explorer 確認訊息

除了直接在程式中接收訊息以外，我們在 Azure 的頁面上其實也能確認傳遞中的訊息內容。

首先到佇列左側的列表找到「**Service Bus Explorer**」，就可以確認目前佇列和無效信件中的訊息數量。

點下從頭查看後，就可以看見訊息列表的內容，並點擊訊息查看本文和屬性：

![Image](https://i.imgur.com/Uy6MDui.png)

在找問題的時候還蠻好用，想確認訊息內容的時候可以試試。

## 建立主題（Topic）

除了佇列（Queue）以外，Service Bus 還提供了另一種傳輸模式：主題（Topic）

讓我們看一下微軟把拔提供的主題概念圖，可以和前面佇列的圖做一下比較：

![Image](https://i.imgur.com/9DwGFgL.png)

簡單來說就是傳送出去之後，會有**多個接收者**等著收訊息。

現在就讓我們回到 Azure Service Bus 的頁面，點選上方的「＋主題」來建立新主題吧：

![Image](https://i.imgur.com/pHdPOvz.png)

> 小提示：主題（Topic）是標準定價層才提供的功能。如果是使用最便宜的基本定價層的朋友，「＋主題」的按鈕應該會反灰的，這時候就要需要從 Service Bus 概觀頁面的「定價層」變更到標準（Standard）才能建立主題。
> 
> 當然不同定價層的價格也會不一樣，要記得確認一下價格呦！

接著就和前面的佇列一樣取個好名字：

![Image](https://i.imgur.com/r51t3f8.png)

建立完就會出現在我們的 ServiceBus 服務中囉：

![Image](https://i.imgur.com/kF998IO.png)

**和單純一對一的佇列不一樣，我們還會需要替主題建立訂用帳戶（Subscriptions）**，這樣主題才知道它到底要把訊息送給哪些對象。

現在讓我們進入主題的頁面，並找到訂用帳戶：

![Image](https://i.imgur.com/vNJpXfl.png)

進去之後讓我們來新增訂用帳戶：

![Image](https://i.imgur.com/wRUeMO2.png)

首先當然是要取個好名字，這邊就先用 Sub1 來當作一號訂閱者的暱稱吧：

![Image](https://i.imgur.com/rQvOrlM.png)

> 這邊和佇列的時候一樣，要注意「最大傳遞計數」（Maximum Delivery Count），簡單來說就是這封訊息會嘗試傳遞幾次，如果超過次數都沒有傳遞成功就會被丟到無效信件。可以參閱 [服務匯流排寄不出的信件佇列的概觀](https://docs.microsoft.com/zh-tw/azure/service-bus-messaging/service-bus-dead-letter-queues#maximum-delivery-count)。


> 另一個要注意的部份是「啟用工作階段」（Session）：簡單來說就是保證訊息的先進先出（FIFO），藉由在訊息中傳遞 SessionID，然後根據訊息的 SessionID 和接收者建立連線之後發送，讓同個工作階段的訊息按照順序發送到同個對象進行處理。
> 
> 這功能在佇列和主題都可以使用，但要定價層在標準和以上才支援。當你有多台機器在接收訊息時會對工作階段比較有感覺（例如說 SessionID 寫死然後開了很多台來處理訊息，結果因為工作階段會全部卡在同一台囧）
> 
> 詳細可以參閱 [Azure 服務匯流排訊息工作階段](https://docs.microsoft.com/zh-tw/azure/service-bus-messaging/message-sessions#session-features)，圖片說明會比較好理解。

建立完畢之後回到訂用帳戶就會看到囉，這邊為了等等能夠示範傳遞訊息給多個訂用者，所以也順便開了 Sub2：

![Image](https://i.imgur.com/q3xkzdQ.png)

這樣就完成了主題的建立啦！接著讓我們回到程式中來撰寫訊息吧～

### 將訊息放入主題

基本上來說，訊息放入主題的方式就和放入佇列的方式一樣：

```csharp
/// <summary>
/// 將訊息放入主題
/// </summary>
[HttpPost]
public async Task Enqueue([FromBody] string context)
{
    // 用 ServiceBus 的連線字串建立 Client
    // 連線字串可以在 Azure ServiceBus 頁面的共用存取原則找到
    // ServiceBusClient 用完記得要呼叫 DisposeAsync() 來關掉
    // 或是直接使用 await using 包起來
    var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
    await using var client = new ServiceBusClient(connectionString);

    // 傳遞 Topic 的名字給 CreateSender 方法來建立 Sender
    // 和 ServiceBusClient 一樣，有提供 DisposeAsync 方法來關閉
    // 或是直接使用 await using 包起來
    var topicName = "YOUR TOPIC NAME";
    await using var sender = client.CreateSender(topicName);

    // 將要傳送的訊息包裝成 ServiceBusMessage
    // 並使用 ServiceBusSender.SendMessageAsync 傳送出去
    var message = new ServiceBusMessage(context);
    await sender.SendMessageAsync(message);
}
```

不用當成大家來找碴，因為真的就是一樣的放法。畢竟 `CreateSender` 的參數名稱是這樣的：

![Image](https://i.imgur.com/nvj36PO.png)

事不宜遲，我們馬上就來傳一封訊息試試：

![Image](https://i.imgur.com/oiL6TD9.png)

這時候再到我們的主題下的訂用帳戶，可以看見訂用帳戶中已經有訊息囉：

![Image](https://i.imgur.com/quyH7n9.png)

因為主題會傳送給所有訂用帳戶，所以兩個訂用帳戶都會分別收到這則訊息，讓我們也確認一眼吧：

![Image](https://i.imgur.com/uoy84gu.png)

看來我們很順利地把訊息傳送出去啦！那因為整批放入的方式也和佇列一樣，這邊就不再贅述。接下來就讓我們把訊息取出來試試吧～

### 從主題中讀取訊息

就像前面寫入訊息到主題和寫入佇列長得九成像一樣，讀取也是差不多的。最大的差別是在**建立 Processor 時，需要多給訂用帳戶名稱**：

```csharp
// 需要同時告訴 Processor 主題名稱和訂用帳戶名稱
var queueName = "YOUR QUEUE NAME";
var subscriptionName = "Sub1";
await using var processor = client.CreateProcessor(topicName, subscriptionName);
```

讓我們修改成 Topic 名稱以及剛剛的訂用帳戶 Sub1 之後執行看看：

![Image](https://i.imgur.com/udHkpxn.png)

可以看見訂用帳戶 Sub1 的訊息也消耗掉了：

![Image](https://i.imgur.com/lpLy4Rd.png)

而訂用帳戶 Sub2 的訊息還在，當我們使用 Sub2 來建立 Processor 並接收訊息之後才會消失：

![Image](https://i.imgur.com/MvOgIL5.png)

其餘的部份都和處理佇列的時候一樣。但為了版面一致 ~~我之後回來複製的時候方便~~ 這邊還是附上程式碼：

```csharp
/// <summary>
/// 取出主題中的訊息
/// </summary>
/// <returns></returns>
[HttpGet]
public async Task Dequeue()
{
    // 和發送訊息的場合差不多：先建立 Client 及 Processor
    var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
    await using var client = new ServiceBusClient(connectionString);

    // 和前面的 ServiceBusSender 一樣，有提供 DisposeAsync 方法讓我們用完時關閉
    // 或是直接使用 await using 包起來
    // 和佇列不一樣的是：需要同時告訴 Processor 主題名稱和訂用帳戶名稱
    var topicName = "YOUR TOPIC NAME";
    var subscriptionName = "YOUR SUBSCRIPTION NAME";
    await using var processor = client.CreateProcessor(topicName, subscriptionName);

    // 告訴 Processor 我們想怎麼處理訊息
    processor.ProcessMessageAsync += MessageHandler;
    processor.ProcessErrorAsync += ErrorHandler;

    // 讓 Processor 上工，開始接收訊息
    await processor.StartProcessingAsync();

    // 實際上會掛著讓 processor 一直處理送來的訊息
    // 這邊就意思意思跑個一下下
    await Task.Delay(1000);

    // 讓 Processor 下班休息
    await processor.StopProcessingAsync();
}

/// <summary>
/// 處理主題訊息
/// </summary>
/// <returns></returns>
private static async Task MessageHandler(ProcessMessageEventArgs args)
{
    // 從訊息的 Body 取出我們發送時塞進去的內容
    var message = args.Message.Body.ToString();
    
    // 對訊息內容做你想做的事。這邊就印出來看個一眼意思意思
    Console.WriteLine(message);

    // 告訴 Service Bus 這個訊息有成功處理了
    await args.CompleteMessageAsync(args.Message);
}

/// <summary>
/// 處理主題錯誤訊息
/// </summary>
/// <returns></returns>
private static async Task ErrorHandler(ProcessErrorEventArgs args)
{
    // 從訊息中取出錯誤訊息
    var exception = args.Exception.ToString();

    // 對訊息做錯誤處理，例如存到日誌系統之類的。這邊也印出來看個一眼意思意思
    Console.WriteLine(exception);
}
```

## 稍作整理

我們已經介紹完了 ServiceBus 的兩種主要工具：佇列和主題的基本操作。

接下來我將稍微對現在的範例程式碼做點簡單的整理，給有興趣的朋友參考。

其餘的朋友可以直接跳到最後的[小結](#小結)。

### 使用 IAzureClientFactory 搭配依賴注入來建立 Azure Client

> 這一小節會用到 .Net Core 的依賴注入，還沒有概念的朋友可以參照 [使用 依賴注入 (Dependency Injection) 來解除強耦合吧](/post/2021/11/newbie-6-dependency-injection)

在範例中我們總是直接 `new ServiceBusClient`。但根據[官方建議](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements?tabs=net-standard-sdk-2#reusing-factories-and-clients)，ServiceBusClient 應該只建立一份並重複使用，以避免重新連線之類的效能損失。

針對 `ServiceBusClient` 的建立和管理，我們可以使用 `IAzureClientFactory` 來讓它自動控制，替我們管理 Client 的實例和連線，用起來就像 `HttpClientFactory` 一樣。

要使用 `IAzureClientFactory`，我們需要先安裝 `Microsoft.Extensions.Azure` 套件：

![Image](https://i.imgur.com/vvFZGCE.png)

現在讓我們移動到 `Program.cs` 並加上 ServiceBusClient 的註冊：

```csharp
builder.Services.AddAzureClients(clientsBuilder =>
{
    var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
    
    clientsBuilder
        .AddServiceBusClient(connectionString)
        .WithName("ServiceBusClient");
});
```

看起來應該會像這樣：

![Image](https://i.imgur.com/Uo0IR1g.png)

> 註：示範專案是使用 .Net 6。如果是使用其他版本或其他 DI 工具的朋友，請再按照自己的狀況調整吧！
> 
> 例如在 `HostBuilder` 的場合，可能就要在 `ConfigureServices` 中進行配置等等，只能祝各位好運。

> 註：除了 `WithName` 替 Client 實例取名以外，也可以呼叫 `ConfigureOptions` 來對 ServiceBusClient 進行各式各樣的設定
> 
> 關於 ServiceBusClient 的設定內容可以參閱 [ServiceBusClientOptions](https://docs.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusclientoptions)
> 
> 而關於 AddAzureClients 的註冊則可以參閱 [How to register ServiceBusClient for dependency injection? - stackoverflow](https://stackoverflow.com/questions/68688838/how-to-register-servicebusclient-for-dependency-injection)

現在我們註冊完了。接著只需要回到使用的類別讓 `IAzureClientFactory` 注入進來就可以囉：

```csharp
/// <summary>
/// Service Bus Queue + IAzureClientFactory 示範用 Controller
/// </summary>
[ApiController]
[Route("[controller]")]
public class QueueWithDiController : ControllerBase
{
    private readonly ServiceBusClient _serviceBusClient;

    public QueueWithDiController(
        IAzureClientFactory<ServiceBusClient> azureClientFactory)
    {
        // 使用注入進來的 IAzureClientFactory 來建立 ServiceBusClient
        _serviceBusClient = azureClientFactory.CreateClient(name: "ServiceBusClient");
    }
}
```

接著我們就可以把原本範例中的呼叫也改成使用這個 Client 來操作囉：
```csharp
var queueName = "YOUR QUEUE NAME";
await using var sender = _serviceBusClient.CreateSender(queueName);
```

> 註：在前面的[官方建議](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements?tabs=net-standard-sdk-2#reusing-factories-and-clients)中，除了 `ServiceBusClient` 以外，其實也建議 `ServiceBusSender`、`ServiceBusReceiver`、`ServiceBusProcessor` 這幾個也應該要維持單例，保持和伺服器的連線以減少建立連線的效能和時間損失。
> 
> `ServiceBusClient` 已經藉由前面提到的 `IAzureClientFactory` 來解決；而像是 `ServiceBusSender` 的後面三項，我們就需要根據專案的狀況來規劃如何重複使用。
> 
> 例如只需要一個 Sender 的場合，我們可以直接在注入的時候註冊成單例；而要和多個 Queue 連線所以需要多個 Sender 的時候，就可以考慮建立一個工廠來管理，並且在工廠裡面優先返回已經建好的實例等等。這部份就請再各位朋友保持柔軟的彈性來處理。

### 將連線字串抽取到 Config

把鏡頭回到我們註冊的地方：
```csharp
builder.Services.AddAzureClients(clientsBuilder =>
{
    var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
    
    clientsBuilder
        .AddServiceBusClient(connectionString)
        .WithName("ServiceBusClient");
});
```

可以看到現在的連線字串是寫死在 `Program.cs` 中的（即使是前面的範例也是直接寫死在 Controller）

但實務上我們大多會將連線字串放在設定檔，例如 Config 或 Appsettings，方便在正式環境或是 Azure 服務上運行時，能夠由外部去設定 or 變更組態來置換連線字串。因此這邊也應該要改成從組態中進行讀取。

首先讓我們把連線字串丟到 `appsettings.json` 的 `ConnectionStrings` 區塊：

```json
"ConnectionStrings": {
    "ServiceBus": "YOUR SERVICE BUS CONNECTION STRING"
}
```

那麼我們就可以更彈性靈活地使用連線字串來註冊 Client 囉：

```csharp
builder.Services.AddAzureClients(clientsBuilder =>
{
    var connectionString = builder.Configuration
        .GetConnectionString("ServiceBus");
    
    clientsBuilder
        .AddServiceBusClient(connectionString)
        .WithName("ServiceBusClient");
});
```

## 小結

這篇我們稍微紀錄了訊息佇列（Message Queue）的用途：拆分生產者和消費者的直接依賴，在中間架設佇列來傳遞訊息。以及這樣做的幾個好處：解除耦合、提高擴展、非同步和提供了緩衝區。

接著介紹了 Azure 的訊息佇列服務：Azure Service Bus，並對其中的兩種模式：佇列和主題，各做了簡單的操作範例。

最後補充了一些範例能優化的方向；使用 IAzureClientFactory 來注入 Client、保持 Sender 等連線重複使用，以及將連線字串拆出到組態處理。

當然還有一些進階的使用場景，例如[交易處理](https://docs.microsoft.com/zh-tw/azure/service-bus-messaging/service-bus-transactions)、[異地複寫](https://docs.microsoft.com/zh-tw/azure/service-bus-messaging/service-bus-geo-dr)等等，但因為這邊還沒有接觸過，就交給需要深入了解的朋朋自行研究囉。

這算是我第一次接觸 Azure 相關的工具，加減筆記一下簡單的使用場景（傳入訊息到佇列／從佇列取出訊息），範例也已經丟到 [Github](https://github.com/Igouist/Demo.AzureServiceBus) 上囉，有缺漏的也歡迎各位朋朋幫忙補充，感謝感謝。 

那麼今天的紀錄就到這邊囉，希望以後還能回來抄。~~總不會筆記寫完就要改用 RabbitMQ 了吧囧~~

## 參考資料

- [[食譜好菜] 比 Azure Queue Storage 功能更完整的 Message Queue 服務 - Azure Service Bus | 軟體主廚的程式料理廚房 - 點部落 (dotblogs.com.tw)](https://www.dotblogs.com.tw/supershowwei/2022/02/13/221639)
- [訊息服務站 - ServiceBus - iT 邦幫忙：：一起幫忙解決難題，拯救 IT 人的一天 (ithome.com.tw)](https://ithelp.ithome.com.tw/articles/10240878)
- [開始使用 Azure 服務匯流排佇列 (.NET) - Azure Service Bus | Microsoft Docs](https://docs.microsoft.com/zh-tw/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues)
- [在 Azure 服務匯流排佇列 (.NET) 中傳送和接收訊息 (github.com)](https://github.com/MicrosoftDocs/azure-docs.zh-tw/blob/master/articles/service-bus-messaging/service-bus-dotnet-get-started-with-queues.md)
- [使用 Python 操作 Azure Service Bus Queues - iT 邦幫忙 (ithome.com.tw)](https://ithelp.ithome.com.tw/articles/10161086)
- [使用 Python 操作 Service Bus Topics/Subscriptions - iT 邦幫忙 (ithome.com.tw)](https://ithelp.ithome.com.tw/articles/10161209)
- [Azure Service Bus（一）入门简介 - Grant_Allen - 博客园 (cnblogs.com)](https://www.cnblogs.com/AllenMaster/p/14000933.html)
- [建立 Azure Service Bus | 程式碼學習不歸路 - 點部落 (dotblogs.com.tw)](https://dotblogs.com.tw/AceLee/2019/07/18/195448)
- [透過 Service Bus Queue trigger Azure Function | 程式碼學習不歸路 - 點部落 (dotblogs.com.tw)](https://dotblogs.com.tw/AceLee/2019/07/18/205733)
- [比較 Azure 佇列儲存體和服務匯流排佇列 - Azure Service Bus | Microsoft Docs](https://docs.microsoft.com/zh-tw/azure/service-bus-messaging/service-bus-azure-and-service-bus-queues-compared-contrasted)
- [比較 Azure 傳訊服務 - Azure Event Grid | Microsoft Docs](https://docs.microsoft.com/zh-tw/azure/event-grid/compare-messaging-services)

