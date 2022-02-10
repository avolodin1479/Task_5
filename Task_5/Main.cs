using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_5
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            List<Level> levels = GetLevel(doc, "Уровень 1", "Уровень 2");
            List<Wall> walls = CreateWalls(doc, 10000, 5000, levels);

            AddDoor(doc, levels[0], walls[0]);
            
            for (int i = 1; i < walls.Count; i++)
            AddWindow(doc, levels[0], walls[i]);

            return Result.Succeeded;
        }

        private List<Level> GetLevel(Document doc, string lev1, string lev2)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            Level level1 = listLevel
               .Where(el => el.Name.Equals(lev1))
               .FirstOrDefault();
            Level level2 = listLevel
              .Where(el => el.Name.Equals(lev2))
              .FirstOrDefault();
            return listLevel;
        }

        private List<Wall> CreateWalls(Document doc, double width, double depth, List<Level> levels)
        {
            width = UnitUtils.ConvertToInternalUnits(width, UnitTypeId.Millimeters);
            depth = UnitUtils.ConvertToInternalUnits(depth, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;
            Level level1 = levels[0];
            Level level2 = levels[1];

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }
            transaction.Commit();
            return walls;
        }

        private void AddDoor(Document doc, Level level, Wall wall)
        {
            Transaction transaction = new Transaction(doc, "Добавление двери");
            transaction.Start();
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level, StructuralType.NonStructural);
            transaction.Commit();
        }

        private void AddWindow(Document doc, Level level, Wall wall)
        {
            Transaction transaction = new Transaction(doc, "Добавление двери");
            transaction.Start();
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1830 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point3 = new XYZ(0, 0, UnitUtils.ConvertToInternalUnits(800, UnitTypeId.Millimeters));
            XYZ point = ((point1 + point2) / 2) + point3;

            if (!windowType.IsActive)
                windowType.Activate();

            doc.Create.NewFamilyInstance(point, windowType, wall, level, StructuralType.NonStructural);
            transaction.Commit();
        }
    }
}
