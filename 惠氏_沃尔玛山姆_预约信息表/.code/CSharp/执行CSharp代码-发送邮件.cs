public SmtpClient smtp = new SmtpClient();

//代码执行入口，请勿修改或删除
public void Run()
{
    initSMTP(); // 配置SMTP信息
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
            Console.WriteLine(e);
            Console.WriteLine("发送邮件异常：" + e.Message);
            mailSentMessage = string.Format("【邮件发送失败】 {0}：{1}", 邮件主题, e.Message);
        }
    }
    
}
//在这里编写您的函数或者类

public void initSMTP(){
    smtp.Port = Convert.ToInt32(发件箱配置jsonObj["port"]);  
    smtp.Host = 发件箱配置jsonObj["smtpServer"].ToString();
    smtp.EnableSsl = true;  
    smtp.UseDefaultCredentials = true;  
    smtp.Credentials = new System.Net.NetworkCredential(发件箱配置jsonObj["email"].ToString(), 发件箱配置jsonObj["password"].ToString());  
    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;  
}


public void SendMailUse()
{
    string[] strtoArr = 收件人.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries);
    string subject = 邮件主题; //邮件的主题             
    string body = 邮件正文; //发送的邮件正文  

    System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage();
    msg.From = new MailAddress(发件箱配置jsonObj["email"].ToString(), "云扩RPA");
    foreach(string toAddress in strtoArr){
        msg.To.Add(toAddress);
    }

    msg.To.Add(defaultEmailReceiver);
    
    // 抄送人 存在的话
    if(!string.IsNullOrEmpty(抄送人)){
        string[] strCCArr = 抄送人.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries);
            foreach(string ccAddress in strCCArr){
                msg.CC.Add(ccAddress);
            }
    }

    msg.Subject = subject;//邮件标题   
    msg.Body = body;//邮件内容   
    msg.BodyEncoding = System.Text.Encoding.UTF8;//邮件内容编码   
    msg.IsBodyHtml = true;//是否是HTML邮件   
    msg.Priority = MailPriority.High;//邮件优先级
    // string[] fileAttachemnts = new string[] { @"C:\RPA工作目录\雀巢_沃尔玛\结果输出\雀巢山姆订单\2021-12\2021-12-02\Copy of Excel To Order_2021-12-02-15-25-32.xlsx", @"C:\RPA工作目录\雀巢_沃尔玛\导出文件\订单pdf\雀巢沃尔玛订单\KMDC4900581930.pdf" };
    foreach(string file in 附件)
    {
        Console.WriteLine("------附件----{0}", file);
        if(System.IO.File.Exists(file)){
            Attachment fileAttch = new Attachment(file);
            msg.Attachments.Add(fileAttch);
        }else{
            Console.WriteLine("附件文件不存在：{0}", file);
        }
        
    }

    smtp.Send(msg);
    Console.WriteLine("发送成功");
   
}