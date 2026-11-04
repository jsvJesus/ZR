package ui
{
   import com.dvalimona.components.*;
   import flash.display.DisplayObjectContainer;
   import flash.events.Event;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   
   public class LanguageSelector extends HBox
   {
      private var helper:Dictionary;
      
      private var buttons:Array;
      
      private var itemWidth:uint = 50;
      
      private var preHeight:uint = 90;
      
      private var itemSpacing:uint = 1;
      
      private var Locale_current_id:String;
      
      public function LanguageSelector(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0)
      {
         super(param1,param2,param3);
      }
      
      override protected function unfreeze() : void
      {
         this.initLanguages();
      }
      
      private function initLanguages() : void
      {
         var _loc2_:PushButton = null;
         var _loc3_:Label = null;
         var _loc4_:uint = 0;
         var _loc5_:Locale = null;
         Locale.core.addEventListener(Locale.CHANGED,this.onLocaleChanged);
         var _loc1_:uint = Locale.locales.length * this.itemWidth + (Locale.locales.length - 1) * this.itemSpacing;
         Logger.LogToChannel(Logger.LOCALIZATION,this.width,this.height);
         this.helper = new Dictionary();
         this.buttons = new Array();
         this.spacing = this.itemSpacing;
         for each(_loc5_ in Locale.locales)
         {
            _loc2_ = new ClearButton(this,0,0,_loc5_.shortcut,this.doLocaleSwitch);
            _loc2_.setSize(30,26);
            this.helper[_loc2_] = _loc5_.id;
            this.buttons.push(_loc2_);
            this.makeLanguageButton(_loc2_);
            if(++_loc4_ < Locale.locales.length)
            {
               _loc3_ = new Label(this);
               _loc3_.size = 21;
               _loc3_.color = 11184810;
               _loc3_.setSize(30,26);
               _loc3_.text = "|";
            }
         }
         if(!Base.isScaleform)
         {
            _loc2_ = new ClearButton(this,0,0,"RU",this.doLocaleSwitch);
            _loc2_.setSize(30,26);
            this.helper[_loc2_] = "RU";
            this.buttons.push(_loc2_);
            this.makeLanguageButton(_loc2_);
            _loc3_ = new Label(this);
            _loc3_.size = 18;
            _loc3_.color = 11184810;
            _loc3_.setSize(30,26);
            _loc3_.text = "|";
            _loc2_ = new ClearButton(this,0,0,"EN",this.doLocaleSwitch);
            _loc2_.setSize(30,26);
            this.helper[_loc2_] = "EN";
            this.buttons.push(_loc2_);
            this.makeLanguageButton(_loc2_);
         }
         this.actualize();
      }
      
      protected function makeLanguageButton(param1:PushButton) : void
      {
         param1.labelColorAlpha = 0.5;
         param1.disabledAlpha = 1;
         param1.disabledLabelAlpha = 1;
      }
      
      protected function onLocaleChanged(param1:Event) : void
      {
         Logger.LogToChannel(Logger.LOCALIZATION,"LanguageSelector catch changes:",Locale.current.id);
         this.actualize();
      }
      
      private function actualize(... rest) : void
      {
         var _loc2_:PushButton = null;
         Logger.LogToChannel(Logger.LOCALIZATION,"LanguageSelector.actualize");
         for each(_loc2_ in this.buttons)
         {
            if(Locale.current)
            {
               _loc2_.enabled = this.helper[_loc2_] !== Locale.current.id;
            }
            _loc2_.tabEnabled = false;
         }
      }
      
      private function doLocaleSwitch(param1:Event) : void
      {
         var lid:String = null;
         var event:Event = param1;
         Logger.LogToChannel(Logger.LOCALIZATION,this.helper[event.target]);
         if(!Base.isScaleform)
         {
            trace(this.helper[event.target]);
            this.Locale_current_id = this.helper[event.target];
            this.actualize();
         }
         else
         {
            lid = this.helper[event.target];
            Base.navigator.showDialog("extendedGUI.Dialogs.Warning","extendedGUI.Dialogs.needRestartLocale",true,[new DialogButtonItem("extendedGUI.Dialogs.restart",function():void
            {
               Locale.setLocaleById(lid);
            },0.61),new DialogButtonItem("extendedGUI.Dialogs.Cancel",null,0.39)]);
         }
      }
   }
}

