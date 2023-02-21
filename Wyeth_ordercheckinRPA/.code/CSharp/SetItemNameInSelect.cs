//代码执行入口，请勿修改或删除
public void Run()
{
    XmlDocument doc = new XmlDocument();
    doc.LoadXml(selectText);

    nodes = doc.DocumentElement.SelectNodes("option");
    Console.WriteLine("Nodes Count : " + nodes.Count);
    
    int nodeIndex = 0;
 foreach (XmlNode node in nodes)
{
    string innerText = node.InnerText.Trim();
    innerText = innerText.Split('-')[0];
    if (innerText.Equals(item["itemName"].ToString().Trim()))
    {
        Console.WriteLine($"innerText sku : {innerText}");
        selectInputText = node.InnerText;
    }
    nodeIndex++;
}
}