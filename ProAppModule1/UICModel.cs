using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Data;

namespace ProAppModule1
{
    class UICModel
    {
        private static readonly UICModel instance = new UICModel();
        //private string uicFacilityId;

        private UICModel() { }

        public static UICModel Instance
        {
            get
            {
                return instance;
            }
        }
        #region UicFacility
        public string UicFacilityId { get; set; }
        public string CountyFips { get; set; }
        public string NaicsPrimary { get; set; }
        public string FacilityName { get; set; }
        public string FacilityAddress { get; set; }
        public string FacilityCity { get; set; }
        public string FacilityState { get; set; }
        public string FacilityZip { get; set; }
        public string FacilityMilePost { get; set; }
        public string Comments { get; set; }




        public Task UpdateUicFacility(string facilityId)
        {
            return QueuedTask.Run(() =>
            {
                var map = MapView.Active.Map;
                FeatureLayer uicFacilities = (FeatureLayer)map.FindLayers("UICFacility").First();
                QueryFilter qf = new QueryFilter()
                {
                    WhereClause = string.Format("FacilityID = '{0}'", facilityId)
                };
                using (RowCursor cursor = uicFacilities.Search(qf))
                {
                    bool hasRow = cursor.MoveNext();
                    using (Row row = cursor.Current)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("UpdateUicFacility: facId: {0} county: {1}",
                                                            row["FacilityID"],
                                                            row["CountyFIPS"]));

                        this.UicFacilityId = Convert.ToString(row["FacilityID"]);
                        this.CountyFips = Convert.ToString(row["CountyFIPS"]);
                        this.NaicsPrimary = Convert.ToString(row["NAICSPrimary"]);
                        this.FacilityName = Convert.ToString(row["FacilityName"]);
                        this.FacilityAddress = Convert.ToString(row["FacilityAddress"]);
                        this.FacilityCity = Convert.ToString(row["FacilityCity"]);
                        this.FacilityState = Convert.ToString(row["FacilityState"]);
                        this.FacilityMilePost = Convert.ToString(row["FacilityMilePost"]);
                        this.Comments = Convert.ToString(row["Comments"]);
                    }
                }
            });
        }

        public bool IsCountyFipsComplete()
        {
            return !String.IsNullOrEmpty(this.CountyFips) && this.CountyFips.Length == 5;
        }
        #endregion UicFacility

        #region UicWell
        public string WellId { get; set; }
        public string WellName { get; set; }
        public string WellClass { get; set; }
        public string WellSubClass { get; set; }
        public string HighPriority { get; set; }
        public string WellSWPZ { get; set; }


        public Task UpdateUicWell(string wellId)
        {
            return QueuedTask.Run(() =>
            {
                var map = MapView.Active.Map;
                FeatureLayer uicWells = (FeatureLayer)map.FindLayers("UICWell").First();
                QueryFilter qf = new QueryFilter()
                {
                    WhereClause = string.Format("FacilityID = '{0}'", wellId)
                };
                using (RowCursor cursor = uicWells.Search(qf))
                {
                    bool hasRow = cursor.MoveNext();
                    using (Row row = cursor.Current)
                    {
                        //System.Diagnostics.Debug.WriteLine(string.Format("UpdateUicFacility: facId: {0} county: {1}",
                        //                                    row["FacilityID"],
                        //                                    row["CountyFIPS"]));

                        this.UicFacilityId = Convert.ToString(row["WellID"]);
                        this.CountyFips = Convert.ToString(row["WellName"]);
                        this.NaicsPrimary = Convert.ToString(row["WellClass"]);
                        this.FacilityName = Convert.ToString(row["WellSubClass"]);
                        this.FacilityAddress = Convert.ToString(row["HighPriority"]);
                        this.FacilityCity = Convert.ToString(row["WellSWPZ"]);
                    }
                }
            });
        }

        public bool IsWellAtributesComplete()
        {
            return !String.IsNullOrEmpty(this.WellId) &&
                   !String.IsNullOrEmpty(this.WellName) &&
                   !String.IsNullOrEmpty(this.WellClass) &&
                   !String.IsNullOrEmpty(this.WellSubClass) &&
                   !String.IsNullOrEmpty(this.HighPriority) &&
                   !String.IsNullOrEmpty(this.WellSWPZ);
        }
        #endregion UicWell

    }
}
