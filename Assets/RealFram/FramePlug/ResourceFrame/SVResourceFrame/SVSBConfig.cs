using System.Xml.Serialization;
using System.Xml;
using System.Collections.Generic;


[System.Serializable]
public class SVSBConfig
{   
    [XmlElement("ABList")]
    public List<ABBase> ABList{ get; set;} 

}
 