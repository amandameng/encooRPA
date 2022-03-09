//代码执行入口，请勿修改或删除
public SmtpClient smtp = new SmtpClient();
public void Run()
{
    #region 配置SMTP信息
    smtp.Port = 587;  
    smtp.Host = "smtp.office365.com";
    smtp.EnableSsl = true;  
    smtp.UseDefaultCredentials = false;  
    smtp.Credentials = new NetworkCredential("rpa@owntrust.cn", "test@2021");  
    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;  
    #endregion
    
    #region 配置Clean List邮件内容
    //For Clean Mail
    try {
        MailMessage cleanMsg = new MailMessage();  
        cleanMsg.From = new MailAddress("rpa@owntrust.cn");
        if(mailSettingTable.AsEnumerable().Cast<DataRow>().Any(s => s["Order_Category"].ToString() == "Clean Order")){
            string toStr = mailSettingTable.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Order_Category"].ToString() == "Clean Order")["Mail_Receipt_Address"].ToString();
            if(toStr.IndexOf("/") > -1){
                var toArray = toStr.Split('/');
                foreach(string toMail in toArray) {
                    if(!string.IsNullOrEmpty(toMail)) cleanMsg.To.Add(new MailAddress(toMail));
                }
            }
            else cleanMsg.To.Add(new MailAddress(toStr));  
        }
        else{
            throw new Exception("全家Clean Order邮件未填写收件人");
        }
        cleanMsg.Subject = "雀巢全家Clean order list";  
        cleanMsg.IsBodyHtml = true; //to make message body as html    

        if(!string.IsNullOrEmpty(cleanSQL)){
            cleanMsg.Body = string.Format(@"<p>Dear All，</p>
                                    <p>附件为本时段全家clean order list，请参协助处理，谢谢。</p>
                                    RPA机器人");
            System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(cleanFilePath);
            cleanMsg.Attachments.Add(attachment);
            if(isResultShown){
                System.Net.Mail.Attachment orderAttachment = new System.Net.Mail.Attachment(pdf订单文件);
                cleanMsg.Attachments.Add(orderAttachment);
          }
        }
        else{
                cleanMsg.Body = string.Format(@"<p>Dear All，</p>
                                    <p>本时段全家系统无新增clean order list，请知悉，谢谢。</p>
                                    RPA机器人");
        }              
        sendMsg(cleanMsg);
       // smtp.Send(cleanMsg);
    } 
    catch (Exception e) {
        Console.WriteLine(e.Message);
        throw new Exception("全家Clean Order邮件发送失败：" + e.Message);
    }
    #endregion
    
    #region 配置 Exception List邮件内容
    //For Exception Mail
    try {
        MailMessage exceptionMsg = new MailMessage();  
        exceptionMsg.From = new MailAddress("rpa@owntrust.cn");  
        if(mailSettingTable.AsEnumerable().Cast<DataRow>().Any(s => s["Order_Category"].ToString() == "Exception Order")){
            string toStr = mailSettingTable.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Order_Category"].ToString() == "Exception Order")["Mail_Receipt_Address"].ToString();
            if(toStr.IndexOf("/") > -1){
                var toArray = toStr.Split('/');
                foreach(string toMail in toArray) {
                    if(!string.IsNullOrEmpty(toMail)) exceptionMsg.To.Add(new MailAddress(toMail));
                }
            }
            else exceptionMsg.To.Add(new MailAddress(toStr));  
        }
        else{
            throw new Exception("全家Exception Order邮件未填写收件人");
        }
        exceptionMsg.Subject = "雀巢全家Exception order list";  
        exceptionMsg.IsBodyHtml = true; //to make message body as html    
        if(!string.IsNullOrEmpty(exceptionSQL)){
            exceptionMsg.Body = string.Format(@"<p>Dear All，</p>
                                    <p>附件为本时段全家exception order list，请参协助处理，谢谢。</p>
                                    RPA机器人");
          
            System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(exceptionFilePath);
            exceptionMsg.Attachments.Add(attachment);
            if(isResultShown){
                System.Net.Mail.Attachment orderAttachment = new System.Net.Mail.Attachment(pdf订单文件);
                exceptionMsg.Attachments.Add(orderAttachment);
           }
        }
        else{
            exceptionMsg.Body = string.Format(@"<p>Dear All，</p>
                                    <p>本时段全家系统无新增exception order list，请知悉，谢谢。</p>
                                    RPA机器人");
        }

        sendMsg(exceptionMsg);
        // smtp.Send(exceptionMsg);  
    } 
    catch (Exception e) {
        throw new Exception("全家Exception邮件发送失败：" + e.Message);
    }
    #endregion
    
    #region 配置Excel To List邮件内容
    //For Excel To Mail
    try {
        MailMessage excelMsg = new MailMessage();  
        excelMsg.From = new MailAddress("rpa@owntrust.cn");  
        if(mailSettingTable.AsEnumerable().Cast<DataRow>().Any(s => s["Order_Category"].ToString() == "Excel To Order")){
            string toStr = mailSettingTable.AsEnumerable().Cast<DataRow>().FirstOrDefault(s => s["Order_Category"].ToString() == "Excel To Order")["Mail_Receipt_Address"].ToString();
            if(toStr.IndexOf("/") > -1){
                var toArray = toStr.Split('/');
                foreach(string toMail in toArray) {
                    if(!string.IsNullOrEmpty(toMail)) excelMsg.To.Add(new MailAddress(toMail));
                }
            }
            else excelMsg.To.Add(new MailAddress(toStr));  
        }
        else{
            throw new Exception("全家Excel To Order邮件未填写收件人");
        }
        excelMsg.Subject = "雀巢全家Excel to order list";  
        excelMsg.IsBodyHtml = true; //to make message body as html    

        if(!string.IsNullOrEmpty(excelSQL)){
            excelMsg.Body = string.Format(@"<p>Dear All，</p>
                                    <p>附件为本时段全家excel to order list，请参协助处理，谢谢。</p>
                                    RPA机器人");
            System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(excelFilePath);
            excelMsg.Attachments.Add(attachment);
            if(isResultShown){
                System.Net.Mail.Attachment orderAttachment = new System.Net.Mail.Attachment(pdf订单文件);
                excelMsg.Attachments.Add(orderAttachment);
            }
        }
        else{
            excelMsg.Body = string.Format(@"<p>Dear All，</p>
                                    <p>本时段全家系统无新增excel to order list，请知悉，谢谢。</p>
                                    RPA机器人");
        }

        sendMsg(excelMsg);
        // smtp.Send(excelMsg);  
    } 
    catch (Exception e) {
        throw new Exception("全家Excel To Order邮件发送失败：" + e.Message);
    }  
    #endregion
    
    #region 解析失败订单发送邮件
    try{
        if(解析失败订单!=null && 解析失败订单.Count > 0){
            MailMessage parseFailedMsg = new MailMessage();  
            parseFailedMsg.From = new MailAddress("rpa@owntrust.cn");
            parseFailedMsg.Subject = "雀巢全家 解析异常订单";
            parseFailedMsg.Body = string.Format(@"<p>Dear All，</p>
                                    <p>以下为本次解析失败订单：</p><br/>
                                    {0}<br/>
                                    RPA机器人", string.Join("<br/>", 解析失败订单));
            if(flowAlertReceiverEmailAddress.IndexOf(";") > -1){
                var toArray = flowAlertReceiverEmailAddress.Split(';');
                foreach(string toMail in toArray) {
                    if(!string.IsNullOrEmpty(toMail)) parseFailedMsg.To.Add(new MailAddress(toMail));
                }
            }else parseFailedMsg.To.Add(new MailAddress(flowAlertReceiverEmailAddress));
            parseFailedMsg.IsBodyHtml = true; //to make message body as html
            sendMsg(parseFailedMsg);
            //smtp.Send(parseFailedMsg);  
        }
    }catch(Exception e){
        Console.WriteLine(e);
        throw new Exception("全家解析异常订单邮件发送失败：" + e.Message);
    }
    
    #endregion
}
//在这里编写您的函数或者类

public void sendMsg(MailMessage msg){
    string curMsg = string.Empty;
    // 邮件重试发送3次
    foreach(int i in new int[1,2,3]){
        try{
            curMsg = string.Empty;
            System.Threading.Thread.Sleep(5000);
            smtp.Send(msg);
            break;
        }
        catch (Exception e)
        {
            curMsg = string.Format("【邮件发送失败】 {0}：{1}", msg.Subject, e.Message);
            Console.WriteLine(curMsg);
        }
    }
    if(!string.IsNullOrEmpty(curMsg)) mailSentMessage += curMsg;
}