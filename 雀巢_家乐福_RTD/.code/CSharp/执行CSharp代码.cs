//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    /**
     *  1. 定义变量 ipAddress
     *  2. 导入 System.Net 程序集
     *  3. 导入 System.Net.Sockets 程序集
     */
    try
    {
        string hostName = Dns.GetHostName(); //得到主机名
        IPHostEntry IpEntry = Dns.GetHostEntry(hostName);
           Console.WriteLine("AddressFamily.InterNetwork：{0}", AddressFamily.InterNetwork);
        for (int i = 0; i < IpEntry.AddressList.Length; i++)
        {
            //从IP地址列表中筛选出IPv4类型的IP地址
            //AddressFamily.InterNetwork表示此IP为IPv4,
            //AddressFamily.InterNetworkV6表示此地址为IPv6类型
            if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
            {
                ipAddress = IpEntry.AddressList[i].ToString();
                Console.WriteLine("ipAddress: {0}", ipAddress);
                return;
            }
        }
        ipAddress = "";
    }
    catch (Exception ex)
    {
        Console.WriteLine("获取IP出错了，详情：" + ex.Message);
        ipAddress = "";
    }
}
//在这里编写您的函数或者类