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
      
      protected var applyButton:MenuButton;
      
      protected var cancelButton:MenuButton;
      
      protected var defaultButton:MenuButton;
      
      protected var backButton:MenuButton;
      
      protected var quality:ItemStepper;
      
      protected var resolution:ItemStepper;
      
      protected var screenMode:ItemStepper;
      
      protected var keyByItem:Dictionary;
      
      protected var itemByKey:Dictionary;
      
      protected var allItems:Array;
      
      protected var targets:Array = ["resolution","quality","screen_mode"];
      
      protected var lastPath:String;
      
      public function SettingsTuneVideoMainScreen(param1:String, param2:uint = 0, param3:Boolean = false)
      {
         super(param1,param2,param3);
      }
      
      override protected function unfreeze(... rest) : void
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
      
      protected function onScreenModeChanged(param1:Event) : void
      {
         var event:Event = param1;
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
      
      private function parseSettingsTune(param1:Object) : void
      {
         this.injectDataByKey(param1,"quality");
         this.injectDataByKey(param1,"resolution");
         this.injectDataByKey(param1,"screen_mode");
         this.checkChanges();
      }
      
      private function injectDataByKey(param1:Object, param2:String) : void
      {
         param2 = param2;
         var _loc3_:Object = param1[param2];
         var _loc4_:ItemStepper = this.itemByKey[param2];
         _loc4_.setByFieldValue("value",_loc3_);
         _loc4_.initValue = _loc3_;
         _loc4_.defaultValue = Settings.Tune.defaultData[param2];
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
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"createStuff",error);
         }
      }
      
      private function createLine(param1:ItemStepper, param2:String) : void
      {
         var key:String = null;
         var value:Object = null;
         var itemBox:HBoxLine = null;
         var itemLabel:LabelShadowed = null;
         var stepperItems:Array = null;
         var range:Object = null;
         var r:String = null;
         var itemStepper:ItemStepper = param1;
         var itemId:String = param2;
         try
         {
            Logger.LogToChannel(Logger.WARNING,"createLine Settings.Tune.data",Settings.Tune.data);
            key = itemId;
            Logger.LogToChannel(Logger.WARNING,"createLine key",key);
            value = Settings.Tune.data[key];
            Logger.LogToChannel(Logger.WARNING,"createLine value",value);
            itemBox = new HBoxLine(this.linesBox);
            itemBox.drawBack = false;
            itemBox.width = widths[0];
            itemBox.height = 35;
            Logger.LogToChannel(Logger.WARNING,"createLine");
            itemLabel = new LabelShadowed();
            itemLabel.size = 22;
            itemLabel.paddingLeft = 0;
            itemLabel.$ = "extendedGUI.Settings." + Settings.Tune.path.join("_") + "_" + StringUtils.dropSpaces(key,"_");
            itemLabel.y = 4;
            itemBox.left.addChild(itemLabel);
            Logger.LogToChannel(Logger.WARNING,"createLine");
            stepperItems = new Array();
            this.itemByKey[key] = itemStepper;
            this.keyByItem[itemStepper] = key;
            this.allItems.push(itemStepper);
            itemStepper.width = widths[0] / 2;
            itemStepper.height = 35;
            itemStepper.paddingRight = 0;
            range = Settings.Tune.range[key];
            Logger.LogToChannel(Logger.WARNING,"createLine");
            for(r in range)
            {
               Logger.LogToChannel(Logger.WARNING,"RANGE",r);
               stepperItems.push({
                  "caption":range[r],
                  "value":String(r),
                  "ranger":parseInt(r)
               });
            }
            Logger.LogToChannel(Logger.WARNING,"createLine");
            itemStepper.items = stepperItems;
            itemStepper.reverseItems();
            itemStepper.addEventListener(Event.CHANGE,this.onStepperItemsChange);
            itemBox.right.addChild(itemStepper);
            Logger.LogToChannel(Logger.WARNING,"createLine");
            itemBox.draw();
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"createLine",error);
         }
      }
      
      override protected function init(... rest) : void
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
         this.defaultButton = new MenuButton(this.vBox);
         this.defaultButton.$ = "extendedGUI.SettingsWindow.setDefault";
         this.defaultButton.height = 35;
         this.defaultButton.addEventListener(MouseEvent.CLICK,this.defaultButtonClickHandler);
         this.applyButton = new MenuButton(this.vBox);
         this.applyButton.$ = "extendedGUI.SettingsWindow.setApplied";
         this.applyButton.height = 35;
         this.applyButton.addEventListener(MouseEvent.CLICK,this.onApplyButtonHandler);
         this.applyButton.enabled = false;
         this.backButton = new MenuButton(this.vBox);
         this.backButton.$ = "extendedGUI.SettingsWindow.backButton";
         this.backButton.height = 35;
         this.backButton.addEventListener(MouseEvent.CLICK,this.onBackButtonHandler);
         this.createStuff();
      }
      
      protected function defaultButtonClickHandler(param1:MouseEvent) : void
      {
         Base.navigator.showDialog("extendedGUI.Settings." + Settings.Tune.path.join("_"),Locale.getById("extendedGUI.SettingsWindow.wantDefault") + "\n" + Locale.getById("extendedGUI.Dialogs.needRestartSettings"),true,[new DialogButtonItem("extendedGUI.SettingsWindow.apply",this.applyDefaultSettings,0.6),new DialogButtonItem("extendedGUI.SettingsWindow.doNotApply",null,0.4)],500,260);
      }
      
      protected function applyDefaultSettings() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"applyDefaultSettings",Settings.Tune.path);
         this.parseSettingsTune(Settings.Tune.defaultData);
         this.makeSettingsRequest(true);
         setTimeout(this.doRestart,100);
      }
      
      protected function get needRestart() : Boolean
      {
         if(this.quality.changed)
         {
            return true;
         }
         return false;
      }
      
      protected function onApplyButtonHandler(param1:Event) : void
      {
         if(this.needRestart)
         {
            Logger.LogToChannel(Logger.WARNING,"need restart");
            Base.navigator.showDialog("extendedGUI.Dialogs.Warning","extendedGUI.Dialogs.needRestartSettings",true,[new DialogButtonItem("extendedGUI.Dialogs.restart",this.makeSettingsRequestAndRestart,0.61),new DialogButtonItem("extendedGUI.Dialogs.Cancel",null,0.39)]);
         }
         else
         {
            this.makeSettingsRequest();
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
      
      private function saveSettingsAndGoBack() : void
      {
         this.makeSettingsRequest();
         this.doGoBack();
      }
      
      private function makeSettingsRequest(param1:Boolean = false) : void
      {
         var _loc2_:Array = null;
         var _loc3_:Object = null;
         var _loc5_:Object = null;
         var _loc6_:Object = null;
         this.applyButton.enabled = false;
         var _loc4_:Array = new Array();
         for each(_loc6_ in this.allItems)
         {
            if(_loc6_.changed == true || param1)
            {
               _loc2_ = Settings.Tune.path.concat([this.keyByItem[_loc6_]]);
               _loc3_ = SettingsObject.makeApiObjectFromPathArray(_loc2_,_loc6_.value);
               Logger.LogToChannel(Logger.DEBUG,"makeSettingsRequest, itm.value",_loc6_.value,Number(_loc6_.value + 1e-7));
               _loc4_.push(_loc3_);
            }
         }
         _loc5_ = SettingsObject.merge(_loc4_);
         Api.call(Api.SET_SETTINGS,[_loc5_]);
         setTimeout(this.requestUpdatedSettings,150);
      }
      
      protected function requestUpdatedSettings() : void
      {
         Settings.self.addEventListener(Settings.READY,this.onSettingsUpdated);
         Settings.GetSettings();
      }
      
      protected function onSettingsUpdated(param1:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onSettingsUpdated");
         Settings.self.removeEventListener(Settings.READY,this.onSettingsUpdated);
         this.updateData();
         this.parseSettingsTune(Settings.Tune.data);
      }
      
      protected function onBackButtonHandler(param1:Event) : void
      {
         this.goBack();
      }
      
      protected function onStepperItemsChange(param1:Event) : void
      {
         switch(param1.target)
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
         this.applyButton.enabled = this.weHaveChanges;
         this.defaultButton.enabled = !this.weHaveDefaults;
      }
      
      private function get weHaveDefaults() : Boolean
      {
         var _loc2_:Object = null;
         Logger.LogToChannel(Logger.DEBUG,"weHaveDefaults length",this.allItems.length);
         var _loc1_:Boolean = true;
         for each(_loc2_ in this.allItems)
         {
            Logger.LogToChannel(Logger.DEBUG,"haveDefaults >",this.keyByItem[_loc2_],_loc2_,_loc2_.isDefaults);
            if(_loc2_.isDefaults == false)
            {
               _loc1_ = false;
               break;
            }
         }
         return _loc1_;
      }
      
      private function get weHaveChanges() : Boolean
      {
         var _loc2_:Object = null;
         var _loc1_:Boolean = false;
         Logger.LogToChannel(Logger.DEBUG,"weHaveChanges length",this.allItems.length);
         for each(_loc2_ in this.allItems)
         {
            Logger.LogToChannel(Logger.DEBUG,"weHaveChanges >",this.keyByItem[_loc2_],_loc2_.value,_loc2_.initValue,_loc2_.changed);
            if(_loc2_.changed == true)
            {
               _loc1_ = true;
               break;
            }
         }
         return _loc1_;
      }
      
      override public function goBack() : void
      {
         if(this.weHaveChanges)
         {
            destroyKeyboardShortcuts();
            Base.navigator.showDialog("extendedGUI.Settings." + Settings.Tune.path.join("_"),"extendedGUI.SettingsWindow.unapplied",true,[new DialogButtonItem("extendedGUI.SettingsWindow.apply",this.saveSettingsAndGoBack,0.4,Keyboard.ENTER),new DialogButtonItem("extendedGUI.SettingsWindow.doNotApply",this.doGoBack,0.6,Keyboard.ESCAPE)],500,200);
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

