using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using System.Collections.ObjectModel;

namespace WpfApp1
{
    class Ifc
    {
        public static int parseFile(string fileName, ref ObservableCollection<Node> nodes, ref int maxLevel)
        {
            int r = -1;
            using (IfcStore model = IfcStore.Open(fileName))
            {
                var project = model.Instances.FirstOrDefault<IIfcProject>();
                readHierarchy(ref nodes, project, 1, ref maxLevel);
                r = 0;
            }
            return r;
        }


        private static void readHierarchy(ref ObservableCollection<Node> nodes, IIfcObjectDefinition o, 
            int level, ref int maxLevel)
        {
            if (level > maxLevel)
                maxLevel = level;

            string volume = "";
            var oProduct = o as IIfcProduct;
            if (oProduct != null)
            {
                readProp(oProduct, out volume);
            }
            Node node = new Node {
                Level = level, Name = o.Name, GlobalId = o.GlobalId,
                TypeName = o.GetType().Name, Volume=volume 
            };
            nodes.Add(node);
            ObservableCollection<Node> subnodes = new ObservableCollection<Node>();
            node.Nodes = subnodes;

            //only spatial elements can contain building elements 
            var oSpatialElement = o as IIfcSpatialStructureElement;
            if (oSpatialElement != null)
            {
                //using IfcRelContainedInSpatialElement to get contained elements 
                var containedElements = oSpatialElement.ContainsElements.SelectMany(rel => rel.RelatedElements);
                foreach (var element in containedElements)
                {
                    readProp(element, out volume);
                    subnodes.Add( new Node { Level=level+1, GlobalId=element.GlobalId, Name=element.Name,
                        TypeName=element.GetType().Name, Volume=volume } );
                }
            }

            //using IfcRelAggregates to get spatial decomposition of spatial structure elements 
            foreach (var item in o.IsDecomposedBy.SelectMany(r => r.RelatedObjects))
            {
                readHierarchy(ref subnodes, item, level + 1, ref maxLevel);
            }
        } // End of readHierarchy


        private static void readProp(IIfcProduct element, out string volume)
        {
            var properties = element.IsDefinedBy.
                Where(r => r.RelatingPropertyDefinition is IIfcPropertySet).
                SelectMany(r => ((IIfcPropertySet)r.RelatingPropertyDefinition).HasProperties).
                OfType<IIfcPropertySingleValue>();
            volume = "";
            foreach (var property in properties)
            {
                //Console.WriteLine("Name=" + property.Name + ", value=" + property.NominalValue.ToString());
                if ((property.Name).ToString().ToLower() == "volume")
                {
                    volume = property.NominalValue.ToString();
                    return;
                }
            }
            var properties2 = element.IsDefinedBy.
                Where(r => r.RelatingPropertyDefinition is IIfcPropertySet).
                SelectMany(r => ((IIfcPropertySet)r.RelatingPropertyDefinition).HasProperties).
                OfType<IIfcPropertySingleValue>();
            volume = "";
            foreach (var property in properties2)
            {
                //Console.WriteLine("Name=" + property.Name + ", value=" + property.ToString());
                if ((property.Name).ToString().ToLower() == "volume")
                {
                    volume = property.NominalValue.ToString();
                    return;
                }
            }
        } // End of readProp
    }
}


/*
            try
            {
                using (IfcStore model = IfcStore.Open(sourceFilePath))
                {
                    sourceFileOpened = true;

                    // Creating a ".csv" file for importing operations into Spider Project
                    using (FileStream opFile = File.Create(opFileName))
                    {
                        opFileCreated = true;
                        using (StreamWriter opFileStreamWriter = new StreamWriter(opFile))
                        {
                            opFileStreamWriter.WriteLine("Level" + delimiter + "Code" + delimiter + "Name" +
                                delimiter + "Type" + delimiter + "VolPlan" + delimiter + "f_Model");
                            var project = model.Instances.FirstOrDefault<IIfcProject>();
                            PrintHierarchy(opFileStreamWriter, project, 1);
                        }
                        opFile.Close();
                        log("Success: " + opFileName + " created");
                    }
                    var context = new Xbim3DModelContext(model);
                    context.CreateContext();

                    // Creating a ".wexbim" one
                    using (FileStream wexbimFile = File.Create(wexbimFileName))
                    {
                        using (BinaryWriter wexbimBinaryWriter = new BinaryWriter(wexbimFile))
                        {
                            model.SaveAsWexBim(wexbimBinaryWriter);
                            wexbimBinaryWriter.Close();
                        }
                        wexbimFile.Close();
                        wexbimCreated = true;
                        log("Success: " + wexbimFileName + " created");
                    }

                    // Importing CoBie data into a ".json"...
                    var facilities = new List<Facility>();
                    
                    var ifcToCoBieLiteUkExchanger = new IfcToCOBieLiteUkExchanger(model, facilities);
                    facilities = ifcToCoBieLiteUkExchanger.Convert();

                    var facilityType = facilities.FirstOrDefault();
                    if (facilityType != null)
                    {
                        facilityType.WriteJson(jsonFileName, true);
                        jsonCreated = true;
                        log("Success: " + jsonFileName + " created");
                        facilityType.WriteXml(xmlFileName, true);
                        xmlCreated = true;
                        log("Success: " + xmlFileName + " created");
                        string errMsg;
                        facilityType.WriteCobie("source.xls", out errMsg);
                        if (errMsg.Length > 0)
                            Console.WriteLine(errMsg);
                        else 
                            xlsCreated = true;
                    }
                    
                }
            }
            catch (Exception e)
            {
                string errorMessage = "An error occured!";
                if (!sourceFileOpened)
                {
                    errorMessage += "\n  - Failed to open the source file " + sourceFilePath;
                }
                if (!opFileCreated)
                {
                    errorMessage += "\n  - Failed to create the destination file " + opFileName;
                }
                if (!wexbimCreated)
                {
                    log("\n - Failed to generate a geometry file " + wexbimFileName);
                }
                if (!jsonCreated)
                {
                    log("\n - Failed to generate a json file " + jsonFileName);
                }
                if (!xmlCreated)
                {
                    log("\n - Failed to generate an xml file " + xmlFileName);
                }
                if (!xlsCreated)
                {
                    log("\n - Failed to generate an xls file " + xlsFileName);
                }
                log(errorMessage);
            }
        }

 
*/
