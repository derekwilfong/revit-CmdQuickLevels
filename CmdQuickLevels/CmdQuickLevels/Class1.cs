using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;


namespace QuickLevels
{
    [Transaction(TransactionMode.Manual)]

    public class CmdQuickLevels : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,
            ref string message, ElementSet elements)

        {

            UIApplication app = commandData.Application;
            Document doc = app.ActiveUIDocument.Document;

            IEnumerable<ViewFamilyType> viewFamilyFloorPlan =
               from elem in new FilteredElementCollector(doc)
               .OfClass(typeof(ViewFamilyType))
               let type = elem as ViewFamilyType
               where type.ViewFamily == ViewFamily.FloorPlan
               select type;


            IEnumerable<ViewFamilyType> viewFamilyCeilingPlan =
               from elem in new FilteredElementCollector(doc)
               .OfClass(typeof(ViewFamilyType))
               let type = elem as ViewFamilyType
               where type.ViewFamily == ViewFamily.CeilingPlan
               select type;

            IList<double> myValue = new List<double>();
            ElementCategoryFilter modelLevel = new ElementCategoryFilter(BuiltInCategory.OST_Levels);

            FilteredElementCollector coll = new FilteredElementCollector(doc);
            IList<Element> levelList = coll.WherePasses(modelLevel).ToElements();
            foreach (Element e in levelList)
            {
                Parameter parameterLevel = e.get_Parameter(BuiltInParameter.LEVEL_ELEV);
                if (parameterLevel == null)
                {
                    continue;
                }

                double paramValue = parameterLevel.AsDouble();
                myValue.Add(paramValue);
            }
            double maxValue = myValue.Max();

            double elevation = 20.0;


            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Create New Level");
                try
                {
                    Level level = doc.Create.NewLevel(maxValue + elevation);
                    if (null == level)

                        level.Name = "New Name";

                    ViewPlan.Create(doc, viewFamilyFloorPlan.First().Id, level.Id);
                    ViewPlan.Create(doc, viewFamilyCeilingPlan.First().Id, level.Id);

                    tx.Commit();
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }
            }
            return Result.Succeeded;
        }

    }
}