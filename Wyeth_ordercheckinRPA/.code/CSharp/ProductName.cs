public void Run()
{
XmlDocument doc = new XmlDocument();
doc.LoadXml(selectText);
//XmlNode node = doc.DocumentElement.SelectSingleNode("select");

//nodes = node.ChildNodes;
nodes = doc.DocumentElement.SelectNodes("select");
}