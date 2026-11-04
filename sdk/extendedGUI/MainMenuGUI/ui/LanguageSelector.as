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
      
      private var itemWidth:uint = 40;
      
      private var preHeight:uint = 90;
      
      public var dinamic_width:uint = 90;
      
      private var itemSpacing:uint = 1;
      
      private var Locale_current_id:String;
      
      public function LanguageSelector(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
      }
      
      override protected function unfreeze() : void
      {
         this.initLanguages();
      }
      
      private function initLanguages() : void
      {
         var localeButton:PushButton = null;
         var divider:Label = null;
         var locale:Locale = null;
         var localeId:String = null;
         var ordered:Array = [];
         var order:Array = ["russian","chinese","english"];
         var count:uint = 0;
         Locale.core.addEventListener(Locale.CHANGED,this.onLocaleChanged);
         for each(localeId in order)
         {
            for each(locale in Locale.locales)
            {
               if(locale.id == localeId)
               {
                  ordered.push(locale);
                  break;
               }
            }
         }
         this.helper = new Dictionary();
         this.buttons = new Array();
         this.spacing = this.itemSpacing;
         this.dinamic_width = ordered.length * 30 + Math.max(0,ordered.length - 1) * 10 + Math.max(0,ordered.length * 2 - 2) * this.itemSpacing;
         this.fixedWidth = this.dinamic_width;
         this.setSize(this.dinamic_width,90);
         for each(locale in ordered)
         {
            localeButton = new ClearButton(this,0,0,locale.shortcut,this.doLocaleSwitch);
            localeButton.setSize(30,26);
            this.helper[localeButton] = locale.id;
            this.buttons.push(localeButton);
            this.makeLanguageButton(localeButton);
            count++;
            if(count < ordered.length)
            {
               divider = new Label(this);
               divider.size = 21;
               divider.color = 11184810;
               divider.setSize(10,26);
               divider.text = "|";
            }
         }
         this.actualize();
      }
      
      protected function makeLanguageButton(btn:PushButton) : void
      {
         btn.labelColorAlpha = 0.5;
         btn.disabledAlpha = 1;
         btn.disabledLabelAlpha = 1;
      }
      
      protected function onLocaleChanged(event:Event) : void
      {
         Logger.LogToChannel(Logger.LOCALIZATION,"LanguageSelector catch changes:",Locale.current.id);
         this.actualize();
      }
      
      private function actualize(... args) : void
      {
         var button:PushButton = null;
         Logger.LogToChannel(Logger.LOCALIZATION,"LanguageSelector.actualize");
         for each(button in this.buttons)
         {
            if(Locale.current)
            {
               button.enabled = this.helper[button] !== Locale.current.id;
            }
            button.tabEnabled = false;
         }
         this.fixedWidth = this.dinamic_width;
         this.setSize(this.dinamic_width,90);
         this.draw();
      }
      
      private function doLocaleSwitch(event:Event) : void
      {
         var lid:String = this.helper[event.currentTarget];
         Logger.LogToChannel(Logger.LOCALIZATION,"LanguageSelector switch:",lid);
         if(lid == null || lid == "")
         {
            return;
         }
         if(Locale.current != null && Locale.current.id == lid)
         {
            return;
         }
         Locale.setLocaleById(lid);
      }
   }
}

