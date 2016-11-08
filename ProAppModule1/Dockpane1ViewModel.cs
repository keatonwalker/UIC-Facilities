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
using System.Collections.ObjectModel;
using System.ComponentModel;
using ArcGIS.Desktop.Framework.Dialogs;
using System.Windows.Input;
using ArcGIS.Core.Data;

namespace ProAppModule1
{
    internal class Dockpane1ViewModel : DockPane
    {
        private const string _dockPaneID = "ProAppModule1_Dockpane1";
        private ObservableCollection<WorkTask> _tableTasks;
        private int selectedUicId;
        private UICModel uicModel = UICModel.Instance;

        public Dockpane1ViewModel()
        {
            _tableTasks = new ObservableCollection<WorkTask>();
            WorkTask root = new WorkTask("esri_editing_AttributesDockPane") { Title = "UIC Workflow" };
            WorkTask childItem1 = new WorkTask("esri_editing_AttributesDockPane") { Title = "UIC facility"};
            childItem1.Items.Add(new WorkTask("esri_editing_CreateFeaturesDockPane") { Title = "Create geometry", IsCompelete = () => false });
            childItem1.Items.Add(new WorkTask("esri_editing_AttributesDockPane") { Title = "Add county FIPS", IsCompelete = uicModel.IsCountyFipsComplete });
            childItem1.Items.Add(new WorkTask("esri_editing_AttributesDockPane") { Title = "Populate attributes", IsCompelete = () => false });
            root.Items.Add(childItem1);
            WorkTask childItem2 = new WorkTask("esri_editing_AttributesDockPane") { Title = "UIC Well Point" };
            childItem2.Items.Add(new WorkTask("esri_editing_CreateFeaturesDockPane") { Title = "Create geometry", IsCompelete = () => false });
            childItem2.Items.Add(new WorkTask("esri_editing_AttributesDockPane") { Title = "Populate attributes", IsCompelete = uicModel.IsWellAtributesComplete });
            root.Items.Add(childItem2);
            root.Items.Add(new WorkTask("esri_editing_CreateFeaturesDockPane") { Title = "Next one", Complete = true});
            _tableTasks.Add(root);
        }

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;
            pane.Activate();
        }
        internal static void subRowEvent()
        {
            Dockpane1ViewModel pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID) as Dockpane1ViewModel;
            QueuedTask.Run(() =>
            {
                if (MapView.Active.GetSelectedLayers().Count == 0)
                {
                    MessageBox.Show("Select a feature class from the Content 'Table of Content' first.");
                    Utils.RunOnUIThread(() => {
                        var contentsPane = FrameworkApplication.DockPaneManager.Find("esri_core_contentsDockPane");
                        contentsPane.Activate();
                    });
                    return;
                }
                //Listen for row events on a layer
                var featLayer = MapView.Active.GetSelectedLayers().First() as FeatureLayer;
                var layerTable = featLayer.GetTable();
                pane.SelectedLayer = featLayer as BasicFeatureLayer;

                //subscribe to row events
                var rowCreateToken = RowCreatedEvent.Subscribe(pane.onCreatedRowEvent, layerTable);
                var rowChangedToken = RowChangedEvent.Subscribe(pane.onRowChangedEvent, layerTable);
            });
        }
        protected void onCreatedRowEvent(RowChangedEventArgs args)
        {

            Utils.RunOnUIThread(() => {
                Dockpane1ViewModel.Show();
                var pane = FrameworkApplication.DockPaneManager.Find("esri_editing_AttributesDockPane");
                pane.Activate();
            });
        }
        protected void onRowChangedEvent(RowChangedEventArgs args)
        {
            var row = args.Row;
            UpdateModel(Convert.ToString(row["FacilityID"]));
        }
        private void CheckTaskItems(WorkTask workTask)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("CheckTaskItems: title: {0}, complete: {1}",
                                                             workTask.Title,
                                                             workTask.IsCompelete().ToString()));
    
            foreach (WorkTask item in workTask.Items)
            {
                CheckTaskItems(item);
            }
            workTask.CheckForCompletion();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "Workflow Progress";
        public string Heading
        {
            get { return _heading; }
            set
            {
                SetProperty(ref _heading, value, () => Heading);
            }
        }

        public ObservableCollection<WorkTask> TableTasks
        {
            get { return _tableTasks;  }
        }

        public void showPaneTest(string paneId)
        {
            Utils.RunOnUIThread(() =>
            {
                var pane = FrameworkApplication.DockPaneManager.Find(paneId);
                pane.Activate();
            });

        }
        private string _uicSelection = "";
        public string UicSelection
        {
            get { return _uicSelection; }
            set
            {
                SetProperty(ref _uicSelection, value, () => UicSelection);
                System.Diagnostics.Debug.WriteLine(_uicSelection);
            }
        }
        private BasicFeatureLayer _selectedLayer;
        public BasicFeatureLayer SelectedLayer
        {
            get { return _selectedLayer; }
            set
            {
                SetProperty(ref _selectedLayer, value, () => SelectedLayer);
            }
        }

        private RelayCommand _newSelectionCmd;
        public ICommand SelectUicAndZoom
        {
            get
            {
                if (_newSelectionCmd == null)
                {
                    _newSelectionCmd = new RelayCommand(() => ModifyLayerSelection(SelectionCombinationMethod.New), () => { return MapView.Active != null && SelectedLayer != null && UicSelection != ""; });
                }
                return _newSelectionCmd;
            }
        }
        private Task ModifyLayerSelection(SelectionCombinationMethod method)
        {
            Task t = QueuedTask.Run(() =>
            {
                System.Diagnostics.Debug.WriteLine("Mod Layer Selection");
                if (MapView.Active == null || SelectedLayer == null || UicSelection == null)
                    return;

                //if (!ValidateExpresion(false))
                //    return;
                string wc = string.Format("FacilityID = \"{0}\"", UicSelection);
                System.Diagnostics.Debug.WriteLine(wc);
                SelectedLayer.Select(new QueryFilter() { WhereClause = wc }, method);
                MapView.Active.ZoomToSelected();
            });
            this.UpdateModel(UicSelection);
            this.showPaneTest("esri_editing_AttributesDockPane");
            return t;
        }

        private async void UpdateModel (string uicId)
        {
            await uicModel.UpdateUicFacility(uicId);
            this.CheckTaskItems(TableTasks[0]);
        }

    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class Dockpane1_ShowButton : Button
    {
        protected override void OnClick()
        {
            Dockpane1ViewModel.subRowEvent();
            Dockpane1ViewModel.Show();
        }
    }
    public delegate bool IsTaskCompelted();

    public class WorkTask: INotifyPropertyChanged
    {
        public WorkTask(string activePanel)
        {
            this.Items = new ObservableCollection<WorkTask>();
            this.ActivePanel = activePanel;
            this.IsCompelete = this.AreChildrenComplete;

            this.StatusColor = "Red";
            this.Complete = false;
        }

        public string Title { get; set; }
        public string StatusColor { get; set; }
        public bool Complete { get; set; }
        public string ActivePanel { get; set; }
        public IsTaskCompelted IsCompelete { get; set; }

        bool _isExpanded;
        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                //if (_isExpanded && _parent != null)
                //    _parent.IsExpanded = true;
            }
        }

        bool _isSelected;
        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    if (_isSelected)
                    {
                        try
                        {
                            
                            //var pane = FrameworkApplication.DockPaneManager.Find("ProAppModule1_Dockpane1") as Dockpane1ViewModel;
                            QueuedTask.Run(() => {
                                Utils.RunOnUIThread(() =>
                                {
                                    var pane = FrameworkApplication.DockPaneManager.Find(ActivePanel);
                                    pane.Activate();
                                });
                            });
                            
                        }
                        catch (Exception)
                        {

                            throw;
                        }

                    }
                    this.OnPropertyChanged("IsSelected");

                }
            }
        }

        public bool CheckForCompletion()
        {
            bool complete = this.IsCompelete();
            System.Diagnostics.Debug.WriteLine(String.Format("CheckForCompletion: {0}",
                                               complete.ToString()));
            if (complete)
            {
                this.StatusColor = "Green";
                this.Complete = true;
            }
            else
            {
                this.Complete = false;
                this.StatusColor = "Black";
            }
            this.OnPropertyChanged("Complete");
            return complete;
        }

        public bool AreChildrenComplete()
        {
            bool complete = true;
            foreach (WorkTask child in this.Items)
            {
                if (!child.Complete)
                {
                    return false;
                }
            }
            return complete;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                System.Diagnostics.Debug.WriteLine(Title);
                System.Diagnostics.Debug.WriteLine(IsSelected);
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion // INotifyPropertyChanged Members

        public ObservableCollection<WorkTask> Items { get; set; }
    }
}
