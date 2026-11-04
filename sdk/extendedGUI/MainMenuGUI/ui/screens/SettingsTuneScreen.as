package ui.screens
{
   import com.dvalimona.components.*;
   import com.dvalimona.utils.*;
   import communication.*;
   import events.*;
   import flash.events.*;
   import flash.ui.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   import ui.Screen;
   import ui.components.*;
   
   public class SettingsTuneScreen extends Screen
   {
      protected var vBox:VBox;
      
      protected var linesBox:VBox;
      
      protected var linesBox2:VBox;
      
      protected var quad1:Quad;
      
      protected var applyButton:MenuButton2;
      
      protected var cancelButton:MenuButton2;
      
      protected var defaultButton:MenuButton2;
      
      protected var backButton:MenuButton2;
      
      protected var colunmsBox:HBox;
      
      protected var sourceByItem:Dictionary;
      
      protected var itemBySource:Dictionary;
      
      protected var allItems:Array;
      
      protected var lastPath:String;
      
      protected var btn_kostil:Boolean = false;
      
      public function SettingsTuneScreen(id:String, depth:uint = 0)
      {
         super(id,depth,false);
      }
      
      override protected function unfreeze(... args) : void
      {
         Dummy.visible = false;
         if(this.applyButton)
         {
            this.applyButton.enabled = false;
         }
         this.label = BreadCrumbs.DIV + Locale.getById("extendedGUI.Settings." + Settings.Tune.path.join("_"));
         Logger.LogToChannel(Logger.DEBUG,"SettingsTuneScreen.unfreeze",Settings.Tune.path,Settings.Tune.path.length);
         this.updateData();
         super.unfreeze();
      }
      
      protected function updateData() : void
      {
         Logger.LogToChannel(Logger.WARNING,"isNewSettingsView",this.isNewSettingsView);
         if(this.isNewSettingsView)
         {
            this.reconstruct();
         }
      }
      
      public function reconstruct() : void
      {
         this.clearBox();
         if(Settings.Tune)
         {
            this.parseSettingsTune(Settings.Tune.data);
         }
      }
      
      protected function get isNewSettingsView() : Boolean
      {
         return true;
      }
      
      private function settingsSort(a:String, b:String) : Number
      {
         var name1:* = undefined;
         var name2:String = null;
         name1 = Locale.getById("extendedGUI.Settings." + Settings.Tune.path.join("_") + "_" + StringUtils.dropSpaces(a,"_"));
         name2 = Locale.getById("extendedGUI.Settings." + Settings.Tune.path.join("_") + "_" + StringUtils.dropSpaces(b,"_"));
         if(name1 > name2)
         {
            return 1;
         }
         if(name1 < name2)
         {
            return -1;
         }
         return 0;
      }
      
      private function parseSettingsTune(source:Object) : void
      {
         var index:int;
         var i0:String = null;
         var i1:String = null;
         var item0:Object = null;
         var item1:Object = null;
         var menuButton:Component = null;
         var itemBox:HBoxLine = null;
         var itemLabel:LabelShadowed = null;
         var range:* = undefined;
         var half:Number = NaN;
         var a:String = null;
         var box:VBox = null;
         var itemButton:MenuButton2 = null;
         var local_path:String = null;
         var key_name:String = null;
         var clickFunc:Function = null;
         var clickFunct2:Function = null;
         var itemSlider:HUISlider = null;
         var stepperItems:Array = null;
         var itemStepper:ItemStepper = null;
         var r:String = null;
         appears = [];
         this.allItems = new Array();
         keys = new Array();
         for(key in source)
         {
            try
            {
               if(Settings.Tune.range[key] != null)
               {
                  keys.push(key);
               }
            }
            catch(error:Error)
            {
            }
         }
         keys.sort(this.settingsSort);
         half = 999;
         a = "";
         if(keys.length > 12)
         {
            half = keys.length / 2;
         }
         index = 0;
         for(key in keys)
         {
            index++;
            i0 = keys[key];
            try
            {
               Logger.LogToChannel(Logger.WARNING,"parseSettingsTune, i0:",i0);
               item0 = source[i0];
               range = Settings.Tune.range[i0];
               if(this.itemBySource[i0] != null)
               {
                  appears.push(this.itemBySource[i0]);
                  if(range is Array && range != "btn")
                  {
                     (this.itemBySource[i0] as HUISlider).tick = 1;
                     (this.itemBySource[i0] as HUISlider).labelPrecision = 0(this.itemBySource[i0] as HUISlider).value = Number(item0);
                     (this.itemBySource[i0] as HUISlider).initValue = Number(item0);
                     (this.itemBySource[i0] as HUISlider).defaultValue = Settings.Tune.defaultData[i0];
                     this.allItems.push(this.itemBySource[i0] as HUISlider);
                  }
                  if(range is Object && range != "btn")
                  {
                     (this.itemBySource[i0] as ItemStepper).setByFieldValue("value",item0);
                     (this.itemBySource[i0] as ItemStepper).initValue = item0;
                     (this.itemBySource[i0] as ItemStepper).defaultValue = Settings.Tune.defaultData[i0];
                     this.allItems.push(this.itemBySource[i0] as ItemStepper);
                  }
                  appears.push(this.quad1);
                  appears.push(this.defaultButton);
                  appears.push(this.applyButton);
                  appears.push(this.backButton);
               }
               else if(range != null)
               {
                  if(index > half)
                  {
                     box = this.linesBox2;
                  }
                  else
                  {
                     box = this.linesBox;
                  }
                  itemBox = new HBoxLine();
                  itemBox.drawBack = false;
                  itemBox.height = 35;
                  if(item0 == "btn")
                  {
                     if(this.btn_kostil)
                     {
                        continue;
                     }
                     itemButton = new MenuButton2();
                     local_path = "extendedGUI.Settings." + Settings.Tune.path.join("_") + "_" + StringUtils.dropSpaces(i0,"_");
                     itemButton.size = 22;
                     itemButton.paddingLeft = 0;
                     itemButton.y = 7;
                     itemButton.$ = local_path;
                     key_name = Settings.Tune.path.join("_") + "_" + StringUtils.dropSpaces(i0,"_");
                     clickFunc = function():void
                     {
                        Api.call(key_name);
                     };
                     clickFunct2 = function():void
                     {
                        Base.navigator.showDialog("extendedGUI.Settings." + Settings.Tune.path.join("_"),Locale.getById("extendedGUI.Settings.ask_are_you_sure"),true,[new DialogButtonItem("extendedGUI.Dialogs.Yes",clickFunc,0.4,[Keyboard.ENTER]),new DialogButtonItem("extendedGUI.Dialogs.No",null,0.6,[Keyboard.ESCAPE])],500,200);
                     };
                     this.sourceByItem[itemButton] = i0;
                     this.allItems.push(itemButton);
                     itemButton.addEventListener(MouseEvent.CLICK,clickFunct2);
                     itemBox.left.addChild(itemButton);
                     this.btn_kostil = true;
                  }
                  else
                  {
                     itemLabel = new LabelShadowed();
                     itemLabel.size = 22;
                     itemLabel.paddingLeft = 0;
                     itemLabel.$ = "extendedGUI.Settings." + Settings.Tune.path.join("_") + "_" + StringUtils.dropSpaces(i0,"_");
                     itemLabel.y = 4;
                     itemBox.left.addChild(itemLabel);
                  }
                  box.addChild(itemBox);
                  Logger.LogToChannel(Logger.DEBUG,"Settings.Tune",i0,"[" + item0 + "]",typeof item0,range);
                  if(range is Array && range != "btn")
                  {
                     Logger.LogToChannel(Logger.DEBUG,"parseSettingsTune: range is Array");
                     itemSlider = new HUISlider(null,0,0,"",null,true);
                     this.itemBySource[i0] = itemSlider;
                     this.sourceByItem[itemSlider] = i0;
                     this.allItems.push(itemSlider);
                     itemSlider.width = widths[0] / 2;
                     itemSlider.height = 35;
                     itemSlider.paddingRight = 0;
                     itemSlider.minimum = range[0];
                     itemSlider.maximum = range[1];
                     switch(range[1])
                     {
                        case 1:
                           itemSlider.labelPrecision = 1;
                           itemSlider.tick = 0.1;
                           break;
                        case 100:
                           itemSlider.labelPrecision = 0;
                           itemSlider.tick = 0.1;
                     }
                     itemSlider.addEventListener(Event.CHANGE,this.onSliderItemsChange);
                     itemSlider.value = Number(item0);
                     itemSlider.initValue = Number(item0);
                     itemSlider.defaultValue = Number(Settings.Tune.defaultData[i0]);
                     itemBox.right.addChild(itemSlider);
                  }
                  else if(range is Object && range != "btn")
                  {
                     stepperItems = new Array();
                     itemStepper = new ItemStepper();
                     this.itemBySource[i0] = itemStepper;
                     this.sourceByItem[itemStepper] = i0;
                     this.allItems.push(itemStepper);
                     itemStepper.width = widths[0] / 2;
                     itemStepper.height = 35;
                     itemStepper.paddingRight = 0;
                     for(r in range)
                     {
                        Logger.LogToChannel(Logger.WARNING,"RANGE",r);
                        stepperItems.push({
                           "caption":range[r],
                           "value":String(r),
                           "ranger":parseInt(r)
                        });
                     }
                     itemStepper.items = stepperItems;
                     if(Settings.Tune.path == "gui")
                     {
                        itemStepper.reverseItems();
                     }
                     itemStepper.setByFieldValue("value",item0);
                     itemStepper.initValue = item0;
                     itemStepper.defaultValue = Settings.Tune.defaultData[i0];
                     itemStepper.addEventListener(Event.CHANGE,this.onStepperItemsChange);
                     itemBox.right.addChild(itemStepper);
                  }
                  appears.push(itemBox);
                  itemBox.draw();
               }
            }
            catch(error:Error)
            {
            }
         }
         this.linesBox.draw();
         this.vBox.draw();
         appears.push(this.quad1);
         appears.push(this.defaultButton);
         appears.push(this.applyButton);
         appears.push(this.backButton);
         appears = [];
         this.checkChanges();
      }
      
      protected function sortedFuction(a:*, b:*) : int
      {
         if(!a.hasOwnProperty("left") || !b.hasOwnProperty("left"))
         {
            return 0;
         }
         var item1:* = a.left.getChildAt(0).$;
         var item2:* = b.left.getChildAt(0).$;
         if(item1 < item2)
         {
            return -1;
         }
         if(item1 > item2)
         {
            return 1;
         }
         return 0;
      }
      
      protected function onSliderItemsChange(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onSliderItemsChange");
         this.checkChanges();
      }
      
      protected function onStepperItemsChange(event:Event) : void
      {
         this.checkChanges();
      }
      
      private function checkChanges() : void
      {
         this.applyButton.enabled = this.weHaveChanges;
         this.defaultButton.enabled = true;
      }
      
      private function get weHaveDefaults() : Boolean
      {
         var itm:Object = null;
         var haveDefaults:Boolean = true;
         for each(itm in this.allItems)
         {
            if(itm.isDefaults == false)
            {
               haveDefaults = false;
               break;
            }
         }
         return haveDefaults;
      }
      
      private function get weHaveChanges() : Boolean
      {
         var itm:Object = null;
         var haveChanges:Boolean = false;
         for each(itm in this.allItems)
         {
            if(itm.changed == true)
            {
               haveChanges = true;
               break;
            }
         }
         return haveChanges;
      }
      
      protected function clearBox() : void
      {
         while(this.linesBox.numChildren > 0)
         {
            this.linesBox.removeChildAt(0);
         }
         while(this.linesBox2.numChildren > 0)
         {
            this.linesBox2.removeChildAt(0);
         }
         this.btn_kostil = false;
         if(Settings.Tune)
         {
            this.lastPath = Settings.Tune.path.join();
         }
         this.itemBySource = new Dictionary();
         this.sourceByItem = new Dictionary();
         this.linesBox.draw();
         this.linesBox2.draw();
         this.vBox.draw();
      }
      
      override protected function init(... args) : void
      {
         this.vBox = new VBox();
         this.vBox.x = 40;
         this.vBox.y = 100;
         this.vBox.alignment = VBox.LEFT;
         this.vBox.spacing = 1;
         this.vBox.debug = false;
         super.addChild(this.vBox);
         this.colunmsBox = new HBox();
         this.colunmsBox.spacing = 20;
         this.colunmsBox.debug = true;
         this.colunmsBox.width = Base.stage.stageWidth - 50 * 2;
         this.vBox.addChild(this.colunmsBox);
         this.linesBox = new VBox();
         this.linesBox.alignment = VBox.JUSTIFY;
         this.linesBox.spacing = 1;
         this.linesBox.debug = false;
         this.linesBox.width = widths[0];
         this.colunmsBox.addChild(this.linesBox);
         this.linesBox2 = new VBox();
         this.linesBox2.alignment = VBox.JUSTIFY;
         this.linesBox2.spacing = 1;
         this.linesBox2.debug = false;
         this.linesBox2.width = widths[0];
         this.colunmsBox.addChild(this.linesBox2);
         this.quad1 = new Quad(this.vBox);
         this.quad1.width = widths[0];
         this.quad1.height = 10;
         this.defaultButton = new MenuButton2(this.vBox);
         this.defaultButton.$ = "extendedGUI.SettingsWindow.setDefault";
         this.defaultButton.height = 35;
         this.defaultButton.addEventListener(MouseEvent.CLICK,this.defaultButtonClickHandler);
         this.applyButton = new MenuButton2(this.vBox);
         this.applyButton.$ = "extendedGUI.SettingsWindow.setApplied";
         this.applyButton.height = 35;
         this.applyButton.addEventListener(MouseEvent.CLICK,this.onApplyButtonHandler);
         this.applyButton.enabled = false;
         this.backButton = new MenuButton2(this.vBox);
         this.backButton.$ = "extendedGUI.SettingsWindow.backButton";
         this.backButton.height = 35;
         this.backButton.addEventListener(MouseEvent.CLICK,this.onBackButtonHandler);
      }
      
      protected function defaultButtonClickHandler(event:MouseEvent) : void
      {
         var loc_text:String = null;
         loc_text = Locale.getById("extendedGUI.SettingsWindow.wantDefault");
         if(this.needRestart)
         {
            loc_text += "\n" + Locale.getById("extendedGUI.Dialogs.needRestartSettings");
         }
         Base.navigator.showDialog("extendedGUI.Settings." + Settings.Tune.path.join("_"),loc_text,true,[new DialogButtonItem("extendedGUI.SettingsWindow.apply",this.applyDefaultSettings,0.6),new DialogButtonItem("extendedGUI.SettingsWindow.doNotApply",null,0.4)],500,260);
      }
      
      protected function applyDefaultSettings() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"applyDefaultSettings",Settings.Tune.path);
         if(this.isNewSettingsView)
         {
            this.clearBox();
         }
         this.parseSettingsTune(Settings.Tune.defaultData);
         this.makeSettingsRequest(true);
      }
      
      protected function get needRestart() : Boolean
      {
         var itm1:Object = null;
         for each(itm1 in this.allItems)
         {
            if(Boolean(itm1.changed) && this.sourceByItem[itm1] == "sound_type")
            {
               return true;
            }
         }
         return false;
      }
      
      protected function onApplyButtonHandler(event:Event) : void
      {
         if(this.needRestart)
         {
            Logger.LogToChannel(Logger.WARNING,"need restart");
            Base.navigator.showDialog("extendedGUI.Dialogs.Warning","extendedGUI.Dialogs.needRestartSettings",true,[new DialogButtonItem("extendedGUI.Dialogs.restart",this.makeSettingsRequestAndRestart,0.61),new DialogButtonItem("extendedGUI.Dialogs.Cancel",null,0.39)]);
         }
         else
         {
            this.saveSettingsAndGoBack();
         }
      }
      
      private function saveSettingsAndGoBack() : void
      {
         this.makeSettingsRequest();
         this.doGoBack();
      }
      
      private function doRestart() : void
      {
         Api.call(Api.DO_RESTART_GAME,[]);
      }
      
      private function makeSettingsRequestAndRestart() : void
      {
         this.makeSettingsRequest();
         setTimeout(this.doRestart,100);
      }
      
      private function makeSettingsRequest(ignoreChanges:Boolean = false) : void
      {
         var objPath:Array = null;
         var apiObject:Object = null;
         var mergedObject:Object = null;
         var itm:Object = null;
         this.applyButton.enabled = false;
         var objectsToMerge:Array = new Array();
         for each(itm in this.allItems)
         {
            if(itm.changed == true || ignoreChanges)
            {
               objPath = Settings.Tune.path.concat([this.sourceByItem[itm]]);
               apiObject = SettingsObject.makeApiObjectFromPathArray(objPath,itm.value);
               Logger.LogToChannel(Logger.DEBUG,"makeSettingsRequest, itm.value",itm.value,Number(itm.value + 1e-7));
               objectsToMerge.push(apiObject);
            }
         }
         mergedObject = SettingsObject.merge(objectsToMerge);
         Api.call(Api.SET_SETTINGS,[mergedObject]);
         setTimeout(this.requestUpdatedSettings,150);
      }
      
      protected function requestUpdatedSettings() : void
      {
         Settings.self.addEventListener(Settings.READY,this.onSettingsUpdated);
         Settings.GetSettings();
      }
      
      protected function onSettingsUpdated(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onSettingsUpdated");
         Settings.self.removeEventListener(Settings.READY,this.onSettingsUpdated);
         this.updateData();
         if(Settings.Tune)
         {
            this.parseSettingsTune(Settings.Tune.data);
         }
      }
      
      protected function onBackButtonHandler(event:Event) : void
      {
         this.goBack();
      }
      
      override public function goBack() : void
      {
         if(this.weHaveChanges)
         {
            destroyKeyboardShortcuts();
            Base.navigator.showDialog("extendedGUI.Settings." + Settings.Tune.path.join("_"),"extendedGUI.SettingsWindow.unapplied",true,[new DialogButtonItem("extendedGUI.SettingsWindow.apply",this.saveSettingsAndGoBack,0.4,[Keyboard.ENTER]),new DialogButtonItem("extendedGUI.SettingsWindow.doNotApply",this.doGoBack,0.6,[Keyboard.ESCAPE])],500,200);
         }
         else
         {
            this.doGoBack();
         }
      }
      
      protected function doGoBack() : void
      {
         Settings.Tune = null;
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.BASE_SETTINGS));
         BreadCrumbs.Remove(this.breadCrumb);
      }
   }
}

