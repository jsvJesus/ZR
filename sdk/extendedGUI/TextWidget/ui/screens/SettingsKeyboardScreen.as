package ui.screens
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.*;
   import flash.events.*;
   import flash.ui.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   import ui.Screen;
   import ui.components.*;
   
   public class SettingsKeyboardScreen extends Screen
   {
      protected var vBox:VBox;
      
      protected var linesBox:HBox;
      
      protected var applyButton:MenuButton;
      
      protected var cancelButton:MenuButton;
      
      protected var defaultButton:MenuButton;
      
      protected var backButton:MenuButton;
      
      protected var quad1:Quad;
      
      protected var helper:Dictionary;
      
      protected var allItems:Array;
      
      public function SettingsKeyboardScreen(param1:String, param2:uint = 0)
      {
         super(param1,param2);
      }
      
      protected function get columns() : uint
      {
         return 4;
      }
      
      override protected function unfreeze(... rest) : void
      {
         Dummy.visible = false;
         if(this.applyButton)
         {
            this.applyButton.enabled = false;
         }
         Logger.LogToChannel(Logger.DEBUG,"SettingsTuneScreen.unfreeze",Settings.Tune.path,Settings.Tune.path.length);
         this.updateData();
         this.label = BreadCrumbs.DIV + Locale.getById("extendedGUI.Settings." + Settings.Tune.path.join("_"));
         super.unfreeze();
      }
      
      override protected function resize(... rest) : void
      {
         this.updateData();
      }
      
      protected function updateData() : void
      {
         this.clearBox();
         this.parseKeybindsTune();
      }
      
      private function parseKeybindsTune() : void
      {
         var _loc1_:String = null;
         var _loc2_:String = null;
         var _loc3_:Object = null;
         var _loc5_:Keybind = null;
         var _loc6_:KeybindButton = null;
         var _loc8_:uint = 0;
         var _loc12_:uint = 0;
         var _loc13_:VBox = null;
         appears = [];
         this.helper = new Dictionary();
         var _loc4_:uint = 0;
         var _loc7_:Array = [];
         this.allItems = new Array();
         for(_loc1_ in Settings.Default[Settings.Tune.path[0]])
         {
            _loc4_++;
         }
         Logger.LogToChannel(Logger.DEBUG,"keyCount",_loc4_);
         _loc8_ = 0;
         while(_loc8_ < this.columns)
         {
            _loc13_ = new VBox();
            appears.push(_loc13_);
            _loc13_.spacing = 1;
            _loc13_.debug = false;
            _loc13_.alignment = VBox.LEFT;
            _loc13_.width = (fullWidth - this.linesBox.spacing * (this.columns - 1)) / this.columns;
            this.linesBox.addChild(_loc13_);
            _loc8_++;
         }
         var _loc9_:uint = Math.ceil(_loc4_ / this.columns);
         var _loc10_:uint = (fullWidth - this.linesBox.spacing * (this.columns - 1)) / this.columns;
         var _loc11_:uint = (fullHeight - 300 - _loc9_) / _loc9_;
         Logger.LogToChannel(Logger.DEBUG,"bHeight",_loc11_);
         Logger.LogToChannel(Logger.DEBUG,"buttonsPerColumn",_loc9_);
         Logger.LogToChannel(Logger.DEBUG,"fullHeight",fullHeight);
         _loc4_ = 0;
         for(_loc1_ in Settings.Default[Settings.Tune.path[0]])
         {
            _loc3_ = Settings.Default[Settings.Tune.path[0]][_loc1_];
            _loc5_ = new Keybind();
            _loc5_.action = _loc1_;
            _loc5_.shortcuts = _loc3_.actions;
            _loc5_.defaultShortcuts = _loc3_.actions;
            _loc5_.originalShortcuts = _loc3_.actions;
            _loc6_ = new KeybindButton(_loc5_);
            _loc6_.width = _loc10_;
            _loc6_.height = _loc11_;
            this.allItems.push(_loc6_);
            _loc6_.addEventListener(Event.CHANGE,this.onKeyItemChange);
            this.helper[_loc6_] = _loc1_;
            _loc7_.push({
               "ranger":_loc3_.id,
               "button":_loc6_
            });
            _loc4_++;
         }
         _loc7_ = this.sortItems(_loc7_);
         _loc12_ = 0;
         while(_loc12_ < _loc7_.length)
         {
            (this.linesBox.getChildAt(Math.floor(_loc12_ / _loc9_)) as VBox).addChild(_loc7_[_loc12_].button);
            _loc12_++;
         }
         for(_loc1_ in Settings.Data[Settings.Tune.path[0]])
         {
            _loc3_ = Settings.Data[Settings.Tune.path[0]][_loc1_];
            Logger.LogToChannel(Logger.WARNING,"item0",_loc3_,"i0",_loc1_);
            this.updateButtonsToUser(_loc1_ as String,_loc3_);
         }
         this.linesBox.draw();
         this.vBox.draw();
         this.checkChanges();
         appears.push(this.quad1);
         appears.push(this.defaultButton);
         appears.push(this.applyButton);
         appears.push(this.backButton);
      }
      
      protected function sortItems(param1:Array) : Array
      {
         var _loc2_:Array = param1;
         var _loc3_:Array = _loc2_.sortOn("ranger",[Array.NUMERIC]);
         var _loc4_:uint = 0;
         while(_loc4_ < _loc3_.length)
         {
            _loc4_++;
         }
         return _loc3_;
      }
      
      private function updateButtonsToUser(param1:String, param2:*) : void
      {
         var _loc3_:KeybindButton = null;
         var _loc4_:Object = null;
         for each(_loc4_ in this.allItems)
         {
            _loc3_ = _loc4_ as KeybindButton;
            if(_loc3_.keybind.action == param1)
            {
               Logger.LogToChannel(Logger.WARNING,"btn.keybind.action",_loc3_.keybind.action,param1);
               _loc3_.keybind.shortcuts = param2;
               _loc3_.keybind.originalShortcuts = param2;
               _loc3_.updateButtonLabel();
            }
         }
      }
      
      protected function clearBox() : void
      {
         var _loc1_:* = this.linesBox.numChildren - 1;
         while(_loc1_ >= 0)
         {
            this.linesBox.removeChildAt(_loc1_);
            _loc1_--;
         }
         this.linesBox.draw();
         this.vBox.draw();
      }
      
      override protected function init(... rest) : void
      {
         this.vBox = new VBox();
         this.vBox.x = 50;
         this.vBox.y = 100;
         this.vBox.alignment = VBox.LEFT;
         this.vBox.spacing = 10;
         this.vBox.debug = false;
         super.addChild(this.vBox);
         this.linesBox = new HBox();
         this.linesBox.spacing = 20;
         this.linesBox.debug = true;
         this.linesBox.width = Base.stage.stageWidth - 50 * 2;
         this.vBox.addChild(this.linesBox);
         this.quad1 = new Quad(this.vBox);
         this.quad1.width = fullWidth;
         this.quad1.height = 10;
         this.defaultButton = new MenuButton(this.vBox);
         this.defaultButton.$ = "extendedGUI.SettingsWindow.setDefault";
         this.defaultButton.height = 30;
         this.defaultButton.addEventListener(MouseEvent.CLICK,this.defaultButtonClickHandler);
         this.applyButton = new MenuButton(this.vBox);
         this.applyButton.$ = "extendedGUI.SettingsWindow.setApplied";
         this.applyButton.height = 30;
         this.applyButton.addEventListener(MouseEvent.CLICK,this.onApplyButtonHandler);
         this.applyButton.enabled = false;
         this.backButton = new MenuButton(this.vBox);
         this.backButton.$ = "extendedGUI.SettingsWindow.backButton";
         this.backButton.height = 30;
         this.backButton.addEventListener(MouseEvent.CLICK,this.onBackButtonHandler);
         this.clearBox();
         this.parseKeybindsTune();
      }
      
      protected function onKeyItemChange(param1:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"onKeyItemChange");
         this.checkChanges();
      }
      
      private function checkChanges() : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"checkChanges");
         this.defaultButton.enabled = this.weHaveChangesFromDefaults;
         this.applyButton.enabled = this.weHaveChanges;
      }
      
      private function get weHaveChangesFromDefaults() : Boolean
      {
         var _loc1_:Object = null;
         for each(_loc1_ in this.allItems)
         {
            if(_loc1_.defaulted == false)
            {
               return true;
            }
         }
         return false;
      }
      
      private function get weHaveChanges() : Boolean
      {
         var _loc1_:Object = null;
         Logger.LogToChannel(Logger.DEBUG,this,"weHaveChanges");
         for each(_loc1_ in this.allItems)
         {
            if(_loc1_.changed == true)
            {
               return true;
            }
         }
         return false;
      }
      
      protected function defaultButtonClickHandler(param1:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"defaultButtonClickHandler");
         this.resetToDefault();
      }
      
      private function resetToDefault() : void
      {
         var _loc1_:KeybindButton = null;
         Logger.LogToChannel(Logger.DEBUG,"resetToDefault");
         for each(_loc1_ in this.allItems)
         {
            _loc1_.resetDefault();
            _loc1_.updateButtonLabel();
         }
         this.checkChanges();
         this.makeSettingsDefaultsRequest();
      }
      
      private function makeSettingsDefaultsRequest() : void
      {
         var objPath:Array = null;
         var apiObject:Object = null;
         var objectsToMerge:Array = null;
         var mergedObject:Object = null;
         var itm:Object = null;
         try
         {
            objectsToMerge = new Array();
            for each(itm in this.allItems)
            {
               Logger.LogToChannel(Logger.WARNING,"Settings.Tune.path",Settings.Tune.path);
               Logger.LogToChannel(Logger.WARNING,"helper[itm]",this.helper[itm]);
               Logger.LogToChannel(Logger.WARNING,"default",(itm as KeybindButton).keybind.defaultShortcuts);
               objPath = Settings.Tune.path.concat([this.helper[itm]]);
               Logger.LogToChannel(Logger.WARNING,"objPath",objPath);
               apiObject = SettingsObject.makeApiObjectFromPathArray(objPath,(itm as KeybindButton).keybind.defaultShortcuts);
               if((itm as KeybindButton).keybind.allowEdit)
               {
                  objectsToMerge.push(apiObject);
               }
            }
            mergedObject = SettingsObject.merge(objectsToMerge);
            Api.call(Api.SET_SETTINGS,[mergedObject]);
            setTimeout(this.requestUpdatedSettings,50);
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"makeSettingsDefaultsRequest error",error);
         }
      }
      
      private function makeSettingsRequest() : void
      {
         var objPath:Array = null;
         var apiObject:Object = null;
         var objectsToMerge:Array = null;
         var mergedObject:Object = null;
         var itm:Object = null;
         try
         {
            objectsToMerge = new Array();
            for each(itm in this.allItems)
            {
               if(itm.changed == true)
               {
                  Logger.LogToChannel(Logger.WARNING,"Settings.Tune.path",Settings.Tune.path);
                  Logger.LogToChannel(Logger.WARNING,"helper[itm]",this.helper[itm]);
                  objPath = Settings.Tune.path.concat([this.helper[itm]]);
                  Logger.LogToChannel(Logger.WARNING,"objPath",objPath);
                  apiObject = SettingsObject.makeApiObjectFromPathArray(objPath,(itm as KeybindButton).keybind.shortcuts);
                  objectsToMerge.push(apiObject);
               }
            }
            mergedObject = SettingsObject.merge(objectsToMerge);
            Api.call(Api.SET_SETTINGS,[mergedObject]);
            setTimeout(this.requestUpdatedSettings,150);
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"makeSettingsRequest error",error);
         }
      }
      
      protected function requestUpdatedSettings() : void
      {
         Settings.self.addEventListener(Settings.READY,this.onSettingsUpdated);
         Settings.GetSettings();
      }
      
      protected function onSettingsUpdated(param1:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onSettingsUpdated");
         this.updateData();
      }
      
      protected function onApplyButtonHandler(param1:Event) : void
      {
         this.makeSettingsRequest();
      }
      
      protected function onBackButtonHandler(param1:Event) : void
      {
         this.goBack();
      }
      
      override public function goBack() : void
      {
         if(this.weHaveChanges)
         {
            Base.navigator.showDialog("extendedGUI.Settings." + Settings.Tune.path.join("_"),"extendedGUI.SettingsWindow.unapplied",true,[new DialogButtonItem("extendedGUI.SettingsWindow.apply",this.makeSettingsRequest,0.4,Keyboard.ENTER),new DialogButtonItem("extendedGUI.SettingsWindow.doNotApply",this.doGoBack,0.6,Keyboard.ESCAPE)],500,200);
         }
         else
         {
            this.doGoBack();
         }
      }
      
      private function doGoBack() : void
      {
         destroyKeyboardShortcuts();
         Settings.Tune = null;
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.BASE_SETTINGS));
         BreadCrumbs.Remove(this.breadCrumb);
      }
   }
}

