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
   
   public class SettingsTuneVideoMainScreen extends Screen
   {
      protected var vBox:VBox;
      
      protected var linesBox:VBox;
      
      protected var quad1:Quad;
      
      protected var applyButton:MenuButton2;
      
      protected var cancelButton:MenuButton2;
      
      protected var defaultButton:MenuButton2;
      
      protected var backButton:MenuButton2;
      
      protected var quality:ItemStepper;
      
      protected var resolution:ItemStepper;
      
      protected var screenMode:ItemStepper;
      
      protected var contrast:HUISlider;
      
      protected var brightness:HUISlider;
      
      protected var maxFrameRate:HUISlider;
      
      protected var async_task:ItemStepper;
      
      protected var taskbar:ItemStepper;
      
      protected var keyByItem:Dictionary;
      
      protected var itemByKey:Dictionary;
      
      protected var allItems:Array;
      
      protected var lastSettings:Object;
      
      protected var targets:Array = ["resolution","quality","screen_mode","taskbar_visible","ASYNC_TASK_ENABLED","DEFERRED_RENDER"];
      
      protected var lastPath:String;
      
      public function SettingsTuneVideoMainScreen(id:String, depth:uint = 0, use3D:Boolean = false)
      {
         super(id,depth,use3D);
      }
      
      override protected function unfreeze(... args) : void
      {
         this.label = BreadCrumbs.DIV + Locale.getById("extendedGUI.Settings." + Settings.Tune.path.join("_"));
         Dummy.visible = false;
         ScreenMode.self.addEventListener(Event.CHANGE,this.onScreenModeChanged);
         if(this.applyButton)
         {
            this.applyButton.enabled = false;
         }
         this.updateData();
         super.unfreeze();
      }
      
      protected function onScreenModeChanged(event:Event) : void
      {
         try
         {
            Logger.LogToChannel(Logger.WARNING,"onScreenModeChanged",ScreenMode.self.state);
            this.updateData();
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"onScreenModeChanged",error);
         }
      }
      
      protected function updateData() : void
      {
         this.parseSettingsTune(Settings.Tune.data);
      }
      
      private function parseSettingsTune(source:Object) : void
      {
         this.injectDataByKey(source,"quality");
         this.injectDataByKey(source,"resolution");
         this.injectDataByKey(source,"screen_mode");
         this.injectDataByKey(source,"contrast");
         this.injectDataByKey(source,"brightness");
         this.injectDataByKey(source,"maxFrameRate");
         this.injectDataByKey(source,"ASYNC_TASK_ENABLED");
         this.injectDataByKey(source,"DEFERRED_RENDER");
         this.injectDataByKey(source,"taskbar_visible");
         this.checkChanges();
      }
      
      private function injectDataByKey(source:Object, key:String) : void
      {
         var itemStepper:ItemStepper = null;
         key = key;
         var value:Object = source[key];
         var range:* = Settings.Tune.range[key];
         if(range is Array)
         {
            this.itemByKey[key].removeEventListener(Event.CHANGE,this.onSliderItemsChange);
            this.itemByKey[key].tick = 1;
            this.itemByKey[key].labelPrecision = 0;
            this.itemByKey[key].minimum = range[0];
            this.itemByKey[key].maximum = range[1];
            this.itemByKey[key].initValue = value;
            this.itemByKey[key].defaultValue = Settings.Tune.defaultData[key];
            this.itemByKey[key].value = value;
            this.itemByKey[key].addEventListener(Event.CHANGE,this.onSliderItemsChange);
         }
         else if(range is Object)
         {
            itemStepper = this.itemByKey[key];
            this.itemByKey[key].setByFieldValue("value",value);
            this.itemByKey[key].initValue = value;
            this.itemByKey[key].defaultValue = Settings.Tune.defaultData[key];
         }
      }
      
      private function createStuff() : void
      {
         try
         {
            this.allItems = new Array();
            this.itemByKey = new Dictionary();
            this.keyByItem = new Dictionary();
            this.quality = new ItemStepper();
            this.createLine(this.quality,"quality");
            this.resolution = new ItemStepper();
            this.createLine(this.resolution,"resolution");
            this.screenMode = new ItemStepper();
            this.createLine(this.screenMode,"screen_mode");
            this.taskbar = new ItemStepper();
            this.createLine(this.taskbar,"taskbar_visible");
            this.contrast = new HUISlider(null,0,0,"",null,true);
            this.createLine(this.contrast,"contrast");
            this.brightness = new HUISlider(null,0,0,"",null,true);
            this.createLine(this.brightness,"brightness");
            this.maxFrameRate = new HUISlider(null,0,0,"",null,true);
            this.createLine(this.maxFrameRate,"maxFrameRate");
            this.async_task = new ItemStepper();
            this.createLine(this.async_task,"ASYNC_TASK_ENABLED");
            this.async_task = new ItemStepper();
            this.createLine(this.async_task,"DEFERRED_RENDER");
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"createStuff",error);
         }
      }
      
      private function createLine(component:Component, itemId:String) : void
      {
         var key:String = null;
         var value:Object = null;
         var itemBox:HBoxLine = null;
         var itemLabel:LabelShadowed = null;
         var stepperItems:Array = null;
         var range:* = undefined;
         var r:String = null;
         try
         {
            key = itemId;
            value = Settings.Tune.data[key];
            itemBox = new HBoxLine(this.linesBox);
            itemBox.drawBack = false;
            itemBox.width = widths[0];
            itemBox.height = 35;
            itemLabel = new LabelShadowed();
            itemLabel.size = 22;
            itemLabel.paddingLeft = 0;
            itemLabel.$ = "extendedGUI.Settings." + Settings.Tune.path.join("_") + "_" + StringUtils.dropSpaces(key,"_");
            itemLabel.y = 4;
            itemBox.left.addChild(itemLabel);
            stepperItems = new Array();
            this.itemByKey[key] = component;
            this.keyByItem[component] = key;
            this.allItems.push(component);
            range = Settings.Tune.range[key];
            if(range is Array)
            {
               (component as HUISlider).tick = 1;
               (component as HUISlider).labelPrecision = 0;
               (component as HUISlider).width = widths[0] / 2;
               (component as HUISlider).height = 35;
               (component as HUISlider).paddingRight = 0;
               (component as HUISlider).paddingTop = 20;
               (component as HUISlider).addEventListener(Event.CHANGE,this.onSliderItemsChange);
            }
            else if(range is Object)
            {
               (component as ItemStepper).width = widths[0] / 2;
               (component as ItemStepper).height = 35;
               (component as ItemStepper).paddingRight = 0;
               for(r in range)
               {
                  stepperItems.push({
                     "caption":range[r],
                     "value":String(r),
                     "ranger":parseInt(r)
                  });
               }
               (component as ItemStepper).items = stepperItems;
               (component as ItemStepper).reverseItems();
               (component as ItemStepper).addEventListener(Event.CHANGE,this.onStepperItemsChange);
            }
            itemBox.right.addChild(component);
            itemBox.draw();
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"createLine",error);
         }
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
         this.linesBox = new VBox();
         this.linesBox.alignment = VBox.JUSTIFY;
         this.linesBox.spacing = 1;
         this.linesBox.debug = false;
         this.linesBox.width = widths[0];
         this.vBox.addChild(this.linesBox);
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
         this.createStuff();
      }
      
      protected function defaultButtonClickHandler(event:MouseEvent) : void
      {
         Base.navigator.showDialog("extendedGUI.Settings." + Settings.Tune.path.join("_"),Locale.getById("extendedGUI.SettingsWindow.wantDefault") + "\n" + Locale.getById("extendedGUI.Dialogs.needRestartSettings"),true,[new DialogButtonItem("extendedGUI.SettingsWindow.apply",this.applyDefaultSettings,0.6),new DialogButtonItem("extendedGUI.SettingsWindow.doNotApply",null,0.4)],500,260);
      }
      
      protected function applyDefaultSettings() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"applyDefaultSettings",Settings.Tune.path);
         this.parseSettingsTune(Settings.Tune.defaultData);
         this.makeSettingsRequest(true);
         setTimeout(this.doRestart,1000);
      }
      
      protected function get needRestart() : Boolean
      {
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
            this.saveSettings();
         }
      }
      
      private function makeSettingsRequestAndRestart() : void
      {
         this.makeSettingsRequest();
         setTimeout(this.doRestart,100);
      }
      
      private function doRestart() : void
      {
         Api.call(Api.DO_RESTART_GAME,[]);
      }
      
      private function revertSettings() : void
      {
         Api.call(Api.SET_SETTINGS,[this.lastSettings]);
         setTimeout(this.requestUpdatedSettings,150);
      }
      
      private function ask_save_settings() : void
      {
         Base.navigator.showDialog("extendedGUI.Dialogs.Warning","extendedGUI.Dialogs.asKApply",true,[new DialogButtonItem("extendedGUI.SettingsWindow.apply",null,0.4,[Keyboard.ENTER]),new DialogButtonItem("extendedGUI.SettingsWindow.doNotApply",this.revertSettings,0.6,[Keyboard.ESCAPE])],500,260,false,true,false,15,"extendedGUI.Dialogs.apply_video_settings");
      }
      
      private function saveSettings() : void
      {
         this.makeSettingsRequest();
         setTimeout(this.ask_save_settings,100);
      }
      
      private function makeSettingsRequest(ignoreChanges:Boolean = false) : void
      {
         var objPath:Array = null;
         var apiObject:Object = null;
         var mergedObject:Object = null;
         var itm:Object = null;
         this.applyButton.enabled = false;
         var objectsToMerge:Array = new Array();
         var old_objectsToMerge:Array = new Array();
         for each(itm in this.allItems)
         {
            if(itm.changed == true || ignoreChanges)
            {
               objPath = Settings.Tune.path.concat([this.keyByItem[itm]]);
               apiObject = SettingsObject.makeApiObjectFromPathArray(objPath,itm.value);
               old_objectsToMerge.push(SettingsObject.makeApiObjectFromPathArray(objPath,itm.initValue));
               Logger.LogToChannel(Logger.DEBUG,"makeSettingsRequest, itm.value",itm.value,Number(itm.value + 1e-7));
               objectsToMerge.push(apiObject);
            }
         }
         mergedObject = SettingsObject.merge(objectsToMerge);
         this.lastSettings = SettingsObject.merge(old_objectsToMerge);
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
         this.parseSettingsTune(Settings.Tune.data);
      }
      
      protected function onBackButtonHandler(event:Event) : void
      {
         this.goBack();
      }
      
      protected function onSliderItemsChange(event:Event) : void
      {
         var slider:HUISlider = event.currentTarget as HUISlider;
         if(slider == this.maxFrameRate)
         {
            this.checkChanges();
            return;
         }
         Api.call(Api.SET_CONTRAST_BRIGHTNESS,[this.contrast.value,this.brightness.value]);
         this.checkChanges();
      }
      
      protected function onStepperItemsChange(event:Event) : void
      {
         switch(event.target)
         {
            case this.quality:
            case this.resolution:
            case this.screenMode:
         }
         this.checkChanges();
      }
      
      private function checkChanges() : void
      {
         if(this.screenMode.value == "1")
         {
            this.resolution.setByFieldValue("value",this.resolution.initValue);
            this.resolution.enabled = false;
         }
         else
         {
            this.resolution.enabled = true;
         }
         this.taskbar.enabled = this.screenMode.value == "1";
         this.applyButton.enabled = this.weHaveChanges;
         this.defaultButton.enabled = !this.weHaveDefaults;
      }
      
      private function get weHaveDefaults() : Boolean
      {
         var itm:Object = null;
         Logger.LogToChannel(Logger.DEBUG,"weHaveDefaults length",this.allItems.length);
         var haveDefaults:Boolean = true;
         for each(itm in this.allItems)
         {
            Logger.LogToChannel(Logger.DEBUG,"haveDefaults >",this.keyByItem[itm],itm,itm.isDefaults);
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
         Logger.LogToChannel(Logger.DEBUG,"weHaveChanges length",this.allItems.length);
         for each(itm in this.allItems)
         {
            Logger.LogToChannel(Logger.DEBUG,"weHaveChanges >",this.keyByItem[itm],itm.value,itm.initValue,itm.changed);
            if(itm.changed == true)
            {
               haveChanges = true;
               break;
            }
         }
         return haveChanges;
      }
      
      override public function goBack() : void
      {
         if(this.weHaveChanges)
         {
            destroyKeyboardShortcuts();
            Base.navigator.showDialog("extendedGUI.Settings." + Settings.Tune.path.join("_"),"extendedGUI.SettingsWindow.unapplied",true,[new DialogButtonItem("extendedGUI.SettingsWindow.apply",this.saveSettings,0.4,[Keyboard.ENTER]),new DialogButtonItem("extendedGUI.SettingsWindow.doNotApply",this.doGoBack,0.6,[Keyboard.ESCAPE])],500,200);
         }
         else
         {
            this.doGoBack();
         }
      }
      
      protected function doGoBack() : void
      {
         Settings.Tune = null;
         if(this.contrast.initValue != this.contrast.value || this.brightness.initValue != this.brightness.value)
         {
            Api.call(Api.SET_CONTRAST_BRIGHTNESS,[this.contrast.initValue,this.brightness.initValue]);
         }
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.BASE_SETTINGS));
         BreadCrumbs.Remove(this.breadCrumb);
      }
   }
}

