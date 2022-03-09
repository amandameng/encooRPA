//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    foreach(int i in new int[1,2,3]){
        try{
            mailSentMessage = string.Empty;
            System.Threading.Thread.Sleep(5000);
            SendMailUse();
            break;
        }
        catch (Exception e)
        {
           // Console.WriteLine(e);
            Console.WriteLine(e.Message);
            mailSentMessage = string.Format("【邮件发送失败】 {0}：{1}", 邮件主题, e.Message);
        }
    }
    
}
//在这里编写您的函数或者类

public void SendMailUse()
{
    string host = "smtp.office365.com";// 邮件服务器smtp.163.com表示网易邮箱服务器    
    string userName = "rpa@owntrust.cn";// 发送端账号   
    string password = 发件人密码; // "test@2021";// 发送端密码(这个客户端重置后的密码)

    SmtpClient client = new SmtpClient();
    client.DeliveryMethod = SmtpDeliveryMethod.Network;//指定电子邮件发送方式    
    client.Host = host;//邮件服务器
    client.UseDefaultCredentials = true;
    client.Port = 587;
    client.EnableSsl = true;
    client.Credentials = new System.Net.NetworkCredential(userName, password);//用户名、密码

    string strfrom = userName;

    string[] strtoArr = 收件人.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries);
    // string strcc = "1172261995@qq.com";//抄送
    string subject = 邮件主题;//邮件的主题             
    string body = 邮件正文;//发送的邮件正文  

    System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage();
    msg.From = new MailAddress(strfrom, "云扩RPA");
    foreach(string toAddress in strtoArr){
        msg.To.Add(toAddress);
    }

    // msg.CC.Add(strcc);

    msg.Subject = subject;//邮件标题   
    msg.Body = body;//邮件内容   
    msg.BodyEncoding = System.Text.Encoding.UTF8;//邮件内容编码   
    msg.IsBodyHtml = true;//是否是HTML邮件   
    msg.Priority = MailPriority.High;//邮件优先级
    // string[] fileAttachemnts = new string[] { @"C:\RPA工作目录\雀巢_沃尔玛\结果输出\雀巢山姆订单\2021-12\2021-12-02\Copy of Excel To Order_2021-12-02-15-25-32.xlsx", @"C:\RPA工作目录\雀巢_沃尔玛\导出文件\订单pdf\雀巢沃尔玛订单\KMDC4900581930.pdf" };
    foreach(string file in 附件)
    {
        Attachment fileAttch = new Attachment(file);
        msg.Attachments.Add(fileAttch);
    }

    client.Send(msg);
    Console.WriteLine("发送成功");
   
}