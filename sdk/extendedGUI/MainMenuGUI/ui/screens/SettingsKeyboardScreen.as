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
      
      protected var applyButton:MenuButton2;
      
      protected var cancelButton:MenuButton2;
      
      protected var defaultButton:MenuButton2;
      
      protected var backButton:MenuButton2;
      
      protected var quad1:Quad;
      
      protected var helper:Dictionary;
      
      protected var allItems:Array;
      
      public function SettingsKeyboardScreen(id:String, depth:uint = 0)
      {
         super(id,depth);
      }
      
      protected function get columns() : uint
      {
         return 4;
      }
      
      override protected function unfreeze(... args) : void
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
      
      override protected function resize(... args) : void
      {
         this.updateData();
      }
      
      protected function updateData() : void
      {
         if(!this.clearBox())
         {
            return;
         }
         this.parseKeybindsTune();
      }
      
      private function parseKeybindsTune() : void
      {
         var i0:String = null;
         var i1:String = null;
         var item0:Object = null;
         var keybind:Keybind = null;
         var keybindButton:KeybindButton = null;
         var cols:uint = 0;
         var keyNum:uint = 0;
         var column:VBox = null;
         appears = [];
         this.helper = new Dictionary();
         var keyCount:uint = 0;
         var order:Array = [];
         this.allItems = new Array();
         for(i0 in Settings.Default[Settings.Tune.path[0]])
         {
            keyCount++;
         }
         Logger.LogToChannel(Logger.DEBUG,"keyCount",keyCount);
         for(cols = 0; cols < this.columns; cols++)
         {
            column = new VBox();
            appears.push(column);
            column.spacing = 1;
            column.debug = false;
            column.alignment = VBox.LEFT;
            column.width = (fullWidth - this.linesBox.spacing * (this.columns - 1)) / this.columns;
            this.linesBox.addChild(column);
         }
         var buttonsPerColumn:uint = Math.ceil(keyCount / this.columns);
         var bWidth:uint = (fullWidth - this.linesBox.spacing * (this.columns - 1)) / this.columns;
         var bHeight:uint = (fullHeight - 300 - buttonsPerColumn) / buttonsPerColumn;
         Logger.LogToChannel(Logger.DEBUG,"bHeight",bHeight);
         Logger.LogToChannel(Logger.DEBUG,"buttonsPerColumn",buttonsPerColumn);
         Logger.LogToChannel(Logger.DEBUG,"fullHeight",fullHeight);
         keyCount = 0;
         for(i0 in Settings.Default[Settings.Tune.path[0]])
         {
            item0 = Settings.Default[Settings.Tune.path[0]][i0];
            keybind = new Keybind();
            keybind.action = i0;
            keybind.shortcuts = item0.actions;
            keybind.defaultShortcuts = item0.actions;
            keybind.originalShortcuts = item0.actions;
            keybindButton = new KeybindButton(keybind);
            keybindButton.width = bWidth;
            keybindButton.height = bHeight;
            this.allItems.push(keybindButton);
            keybindButton.addEventListener(Event.CHANGE,this.onKeyItemChange);
            this.helper[keybindButton] = i0;
            order.push({
               "ranger":item0.id,
               "button":keybindButton
            });
            keyCount++;
         }
         order = this.sortItems(order);
         for(keyNum = 0; keyNum < order.length; keyNum++)
         {
            (this.linesBox.getChildAt(Math.floor(keyNum / buttonsPerColumn)) as VBox).addChild(order[keyNum].button);
         }
         for(i0 in Settings.Data[Settings.Tune.path[0]])
         {
            item0 = Settings.Data[Settings.Tune.path[0]][i0];
            Logger.LogToChannel(Logger.WARNING,"item0",item0,"i0",i0);
            this.updateButtonsToUser(i0 as String,item0);
         }
         this.linesBox.draw();
         this.vBox.draw();
         this.checkChanges();
         appears.push(this.quad1);
         appears.push(this.defaultButton);
         appears.push(this.applyButton);
         appears.push(this.backButton);
      }
      
      protected function sortItems(nonSorted:Array) : Array
      {
         var temp:Array = nonSorted;
         var sorted:Array = temp.sortOn("ranger",[Array.NUMERIC]);
         for(var i:uint = 0; i < sorted.length; i++)
         {
         }
         return sorted;
      }
      
      private function updateButtonsToUser(action:String, shortcut:*) : void
      {
         var btn:KeybindButton = null;
         var itm:Object = null;
         for each(itm in this.allItems)
         {
            btn = itm as KeybindButton;
            if(btn.keybind.action == action)
            {
               Logger.LogToChannel(Logger.WARNING,"btn.keybind.action",btn.keybind.action,action);
               btn.keybind.shortcuts = shortcut;
               btn.keybind.originalShortcuts = shortcut;
               btn.updateButtonLabel();
            }
         }
      }
      
      protected function clearBox() : Boolean
      {
         if(this.linesBox == null)
         {
            return false;
         }
         for(var i:* = this.linesBox.numChildren - 1; i >= 0; i--)
         {
            this.linesBox.removeChildAt(i);
         }
         this.linesBox.draw();
         this.vBox.draw();
         return true;
      }
      
      override protected function init(... args) : void
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
         this.defaultButton = new MenuButton2(this.vBox);
         this.defaultButton.$ = "extendedGUI.SettingsWindow.setDefault";
         this.defaultButton.height = 30;
         this.defaultButton.addEventListener(MouseEvent.CLICK,this.defaultButtonClickHandler);
         this.applyButton = new MenuButton2(this.vBox);
         this.applyButton.$ = "extendedGUI.SettingsWindow.setApplied";
         this.applyButton.height = 30;
         this.applyButton.addEventListener(MouseEvent.CLICK,this.onApplyButtonHandler);
         this.applyButton.enabled = false;
         this.backButton = new MenuButton2(this.vBox);
         this.backButton.$ = "extendedGUI.SettingsWindow.backButton";
         this.backButton.height = 30;
         this.backButton.addEventListener(MouseEvent.CLICK,this.onBackButtonHandler);
         this.clearBox();
         this.parseKeybindsTune();
      }
      
      protected function onKeyItemChange(event:Event) : void
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
         var itm:Object = null;
         for each(itm in this.allItems)
         {
            if(itm.defaulted == false)
            {
               return true;
            }
         }
         return false;
      }
      
      private function get weHaveChanges() : Boolean
      {
         var itm:Object = null;
         Logger.LogToChannel(Logger.DEBUG,this,"weHaveChanges");
         for each(itm in this.allItems)
         {
            if(itm.changed == true)
            {
               return true;
            }
         }
         return false;
      }
      
      protected function defaultButtonClickHandler(event:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"defaultButtonClickHandler");
         this.resetToDefault();
      }
      
      private function resetToDefault() : void
      {
         var itm:KeybindButton = null;
         Logger.LogToChannel(Logger.DEBUG,"resetToDefault");
         for each(itm in this.allItems)
         {
            itm.resetDefault();
            itm.updateButtonLabel();
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
      
      protected function onSettingsUpdated(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onSettingsUpdated");
         this.updateData();
      }
      
      protected function onApplyButtonHandler(event:Event) : void
      {
         this.makeSettingsRequest();
      }
      
      protected function onBackButtonHandler(event:Event) : void
      {
         this.goBack();
      }
      
      override public function goBack() : void
      {
         if(this.weHaveChanges)
         {
            Base.navigator.showDialog("extendedGUI.Settings." + Settings.Tune.path.join("_"),"extendedGUI.SettingsWindow.unapplied",true,[new DialogButtonItem("extendedGUI.SettingsWindow.apply",this.makeSettingsRequest,0.4,[Keyboard.ENTER]),new DialogButtonItem("extendedGUI.SettingsWindow.doNotApply",this.doGoBack,0.6,[Keyboard.ESCAPE])],500,200);
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

