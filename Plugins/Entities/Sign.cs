﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Plugins.Entities
{
    /// <summary>
    /// Специальный знак
    /// </summary>
    sealed class Sign : Entity
    {
        /// <summary>
        /// Создатель блоков
        /// </summary>
        readonly static BlocksCreater creater;
        /// <summary>
        /// Статическое создание
        /// </summary>
        static Sign()
        {
            creater = new BlocksCreater();
        }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        public Sign(Database db, Primitive draw) : base(db, draw) { }
        /// <summary>
        /// Рисование объекта
        /// </summary>
        public override void Draw()
        {
            var settings = primitive.DrawSettings;
            var key = settings.Value<string>("FontName") + "_" + settings.Value<string>("Symbol");
            if (!HasKey(key) && !creater.Create(key))
                return;

            using (var reference = new BlockReference(Wkt.Lines.ParsePoint(primitive.Geometry), GetByKey(key))
            {
                Color = ColorConverter.FromMMColor(settings.Value<int>(COLOR)),
                Layer = primitive.LayerName,
                ScaleFactors = new Scale3d(settings.Value<string>("FontScaleX").ToDouble())
            })
            {
                AppendToDb(reference);
            }
        }
    }
}