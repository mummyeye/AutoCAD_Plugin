﻿using Plugins.Logging;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Entities
{
    /// <summary>
    /// Объект отрисовки
    /// </summary>
    abstract class Entity
    {
        #region Protected Fields

        // FIXME: ??? Необходимо ли иметь доступ всем предкам к логгеру ???
        protected readonly ILogger logger;
        /// <summary>
        /// Ключевое слово
        /// </summary>
        protected readonly string COLOR = "Color";

        #endregion

        #region Public Fields

        /// <summary>
        /// Параметры отрисовки объекта
        /// </summary>
        public readonly Primitive primitive;

        #endregion

        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="prim">Параметры отрисовки объекта</param>
        public Entity(Primitive prim, ILogger log)
        {
            primitive = prim;
            logger = log;
        }

        #endregion

        #region Protected Methods

        protected abstract void Draw(Transaction transaction, BlockTable table, BlockTableRecord record);

        #endregion

        #region Public Methods

        /// <summary>
        /// Рисование объекта
        /// </summary>
        public void AppendToDrawing(Database db) 
        {
            Transaction transaction = null;

            try
            {
                transaction = db.TransactionManager.StartTransaction();
                var table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                var record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Draw(transaction, table, record);
            }
            catch (NotDrawingLineException) { } // Перехват полилиний с неправильными парамерами
            catch (System.Exception e)
            {
                logger.Log(LogLevel.Error, "", e);
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Commit();
                    transaction.Dispose();
                }
            }
        }

        #endregion
    }
}