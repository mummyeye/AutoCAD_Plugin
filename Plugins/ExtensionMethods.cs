﻿using System;

using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using APolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Autodesk.AutoCAD.DatabaseServices;
using Newtonsoft.Json.Linq;

using static Plugins.Constants;

namespace Plugins
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Конвертация строки в вещественное число
        /// </summary>
        /// <param name="str">Строковое представление числа</param>
        /// <returns>Вещественное число</returns>
        public static double ToDouble(this string str) => System.Convert.ToDouble(str.Replace('.', ','));
        /// <summary>
        /// Конвертация градусов в радианы
        /// </summary>
        /// <param name="degree">Угол в градусах</param>
        /// <returns>Угол в радианах</returns>
        public static double ToRad(this double degree) => degree / 180 * System.Math.PI;
        /// <summary>
        /// Добавить определение поля в таблицу символов
        /// </summary>
        /// <param name="regAppName">Имя поля</param>
        static void AddRegAppTableRecord(string regAppName)
        {
            var db = AApplication.DocumentManager.MdiActiveDocument.Database;
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.RegAppTableId, OpenMode.ForWrite) as RegAppTable;
                if (!table.Has(regAppName))
                {
                    var record = new RegAppTableRecord { Name = regAppName };
                    table.Add(record);
                    transaction.AddNewlyCreatedDBObject(record, true);
                }
                transaction.Commit();
            }
        }
        /// <summary>
        /// Добавление в XData необходимых для связывания таблиц параметров
        /// </summary>
        /// <param name="entity">Связываемый объект</param>
        /// <param name="drawParams">Параметры отрисовки</param>
        public static void AddXData(this Entity entity, Entities.Primitive drawParams)
        {
            AddRegAppTableRecord(SYSTEM_ID);
            AddRegAppTableRecord(BASE_NAME);
            AddRegAppTableRecord(LINK_FIELD);

            var buffer = new ResultBuffer(new TypedValue(1001, SYSTEM_ID),
                                                   new TypedValue((int)DxfCode.ExtendedDataInteger32, drawParams.SystemId));

            if (drawParams.BaseName != null && drawParams.ChildField != null)
            {
                buffer = new ResultBuffer(new TypedValue(1001, SYSTEM_ID),
                                          new TypedValue((int)DxfCode.ExtendedDataInteger32,
                                                         drawParams.SystemId),
                                          new TypedValue(1001, BASE_NAME),
                                          new TypedValue((int)DxfCode.ExtendedDataAsciiString,
                                                         drawParams.BaseName),
                                          new TypedValue(1001, LINK_FIELD),
                                          new TypedValue((int)DxfCode.ExtendedDataAsciiString,
                                                         drawParams.ChildField));
            }

            entity.XData = buffer;
        }
        /// <summary>
        /// Установка свойств для объекта Polyline
        /// </summary>
        /// <param name="polyline">Исходный объект</param>
        /// <param name="settings">Параметры отрисовки</param>
        /// <param name="layer">Слой отрисовки</param>
        public static APolyline SetDrawSettings(this APolyline polyline, JObject settings, string layer)
        {
            const string PEN_COLOR = "PenColor";
            const string WIDTH = "Width";
            const string BORDER_DESCRIPRION = "BorderDescription";

            polyline.Color = ColorConverter.FromMMColor(settings.Value<int>(PEN_COLOR));
            polyline.Thickness = settings.Value<double>(WIDTH);
            polyline.Layer = layer;

            if (settings.TryGetValue(BORDER_DESCRIPRION, StringComparison.CurrentCulture, out JToken borderDescription)
               && borderDescription.Value<string>() == "{D075F160-4C94-11D3-A90B-A8163E53382F}")
            {
                throw new NotDrawingLineException();
            }
            else if (settings.Value<int>("nPenStyle") == 1)
            {
                polyline.Linetype = "MMP_2"; // LineTypeLoader.STYLE_NAME + "1";
            }

            return polyline;
        }
        /// <summary>
        /// Получение XData
        /// </summary>
        /// <param name="buffer">Исходный буфер</param>
        /// <param name="RegAppName">Зарегестрированное имя</param>
        /// <returns></returns>
        public static string GetXData(this ResultBuffer buffer, string RegAppName)
        {
            var flag = false;
            var result = string.Empty;
            foreach (var tv in buffer)
            {
                if (flag)
                {
                    result = tv.Value.ToString();
                    flag = false;
                }
                if ((tv.TypeCode == (short)DxfCode.ExtendedDataRegAppName) && (tv.Value.ToString() == RegAppName))
                {
                    flag = true;
                }
            }
            return result;
        }
        public static void AppendToDb(this Entity entity,
                                      Transaction transaction,
                                      BlockTableRecord record,
                                      Entities.Primitive primitive)
        {
            if (entity is null)
                throw new ArgumentNullException(nameof(entity));

            if (transaction is null)
                throw new ArgumentNullException(nameof(transaction));

            if (record is null)
                throw new ArgumentNullException(nameof(record));

            if (primitive is null)
                throw new ArgumentNullException(nameof(primitive));

            entity.AddXData(primitive);
            record.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
        }
    }
}