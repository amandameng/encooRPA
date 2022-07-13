//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    foreach(int i in new int[1,2,3]){
        try{
            mailSentMessage = string.Empty;
            System.Threading.Thread.Sleep(5000);
            sendMail();
            break;
        }catch(Exception e){
            Console.WriteLine(e);
            Console.WriteLine("发送邮件异常：" + e.Message);
            mailSentMessage = string.Format("【邮件发送失败】 {0}：{1}", 邮件主题, e.Message);
        }
    }     
}
//在这里编写您的函数或者类

public void sendMail(){
    var message = new MimeMessage();
    string fromEmail = 发件箱配置jsonObj["email"].ToString();
    string mailPwd = 发件箱配置jsonObj["password"].ToString();
    message.From.Add(new MailboxAddress("Encoo RPA", fromEmail));

    string[] strtoArr = 收件人.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries);
    foreach(string toAddress in strtoArr){
        message.To.Add(new MailboxAddress(toAddress));
    }

    // 抄送人 存在的话
    if(!string.IsNullOrEmpty(抄送人)){
        string[] strCCArr = 抄送人.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries);
            foreach(string ccAddress in strCCArr){
                message.Cc.Add(new MailboxAddress(ccAddress));
            }
    }

    message.Subject = 邮件主题;
    message.Body = setBody();

    using (var client = new MailKit.Net.Smtp.SmtpClient())
    {
        client.Connect(发件箱配置jsonObj["smtpServer"].ToString(), Convert.ToInt32(发件箱配置jsonObj["port"]), true);

        // Note: only needed if the SMTP server requires authentication
        client.Authenticate(fromEmail, mailPwd);

        client.Send(message);
        client.Disconnect(true);
    }
}
    
    
public MimeEntity setBody()
{
    var builder = new BodyBuilder();

    // Set the plain-text version of the message text
    builder.TextBody = 邮件正文;
    // Set the html version of the message text
    builder.HtmlBody = 邮件正文;
    
    foreach(string path in 附件){
        var attachment = new MimePart("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                Content = new MimeContent(File.OpenRead(path), ContentEncoding.Default),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = Path.GetFileName(path)
            };
        builder.Attachments.Add(attachment);
    }
    // string path = @"C:\RPA工作目录\惠氏_沃尔玛山姆\山姆\2022-05\2022-05-30\Clean_Order_山姆2022-05-30_1.xlsx";
    return builder.ToMessageBody();
}