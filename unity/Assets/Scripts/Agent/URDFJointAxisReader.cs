using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class URDFJointAxisReader
{
    public string urdfFilePath = "Assets/urdf/robot_description/x2/x2.urdf";

    void Start()
    {
        LoadURDF(urdfFilePath);
    }
    
    public static Dictionary<string ,string> LoadURDF(string filePath)
    {
        Dictionary<string,string >jointAxes=new Dictionary<string, string>();
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(filePath);

        XmlNodeList jointNodes = xmlDoc.GetElementsByTagName("joint");
        foreach (XmlNode jointNode in jointNodes)
        {
            string jointName = jointNode.Attributes["name"].Value;
            XmlNode axisNode = jointNode.SelectSingleNode("axis");
            XmlNode linkNode = jointNode.SelectSingleNode("child");
            
            if (axisNode != null&&linkNode != null)
            {
                string[] axisValues = axisNode.Attributes["xyz"].Value.Split(' ');
                Vector3 axis = new Vector3(
                    float.Parse(axisValues[0]),
                    float.Parse(axisValues[1]),
                    float.Parse(axisValues[2])
                );

                string axisString = GetAxisString(axis);
                string linkName = linkNode.Attributes["link"].Value;
                jointAxes[linkName] = axisString;
            }
        }

        return jointAxes;

    }

    private static string GetAxisString(Vector3 axis)
    {
        if (axis.x != 0)
            return axis.x > 0 ? "z" : "-z";
        if (axis.y != 0)
            return axis.y > 0 ? "x" : "-x";
        if (axis.z != 0)
            return axis.z > 0 ? "y" : "-y";
        return "unknown";
    }
} 