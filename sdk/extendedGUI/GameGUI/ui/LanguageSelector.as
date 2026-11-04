package ui
{
   import com.dvalimona.components.ClearButton;
   import com.dvalimona.components.HBox;
   import com.dvalimona.components.Label;
   import com.dvalimona.components.PushButton;
   import flash.display.DisplayObjectContainer;
   import flash.events.Event;
   import flash.utils.Dictionary;
   import lang.Locale;
   import logging.Logger;
   
   public class LanguageSelector extends HBox
   {
      private var helper:Dictionary;
      
      private var buttons:Array;
      
      private var itemWidth:uint = 50;
      
      private var preHeight:uint = 90;
      
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
         var count:uint = 0;
         var locale:Locale = null;
         Locale.core.addEventListener(Locale.CHANGED,this.onLocaleChanged);
         var preWidth:uint = Locale.locales.length * this.itemWidth + (Locale.locales.length - 1) * this.itemSpacing;
         this.setSize(preWidth,90);
         Logger.LogToChannel(Logger.LOCALIZATION,this.width,this.height);
         this.helper = new Dictionary();
         this.buttons = new Array();
         this.spacing = this.itemSpacing;
         for each(locale in Locale.locales)
         {
            localeButton = new ClearButton(this,0,0,locale.shortcut,this.doLocaleSwitch);
            localeButton.setSize(30,26);
            this.helper[localeButton] = locale.id;
            this.buttons.push(localeButton);
            this.makeLanguageButton(localeButton);
            count++;
            if(count < Locale.locales.length)
            {
               divider = new Label(this);
               divider.size = 21;
               divider.color = 11184810;
               divider.setSize(30,26);
               divider.text = "|";
            }
         }
         if(!Base.isScaleform)
         {
            localeButton = new ClearButton(this,0,0,"RU",this.doLocaleSwitch);
            localeButton.setSize(30,26);
            this.helper[localeButton] = "RU";
            this.buttons.push(localeButton);
            this.makeLanguageButton(localeButton);
            divider = new Label(this);
            divider.size = 18;
            divider.color = 11184810;
            divider.setSize(30,26);
            divider.text = "|";
            localeButton = new ClearButton(this,0,0,"EN",this.doLocaleSwitch);
            localeButton.setSize(30,26);
            this.helper[localeButton] = "EN";
            this.buttons.push(localeButton);
            this.makeLanguageButton(localeButton);
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
            trace(this.helper[button],this.Locale_current_id);
            button.enabled = this.helper[button] !== Locale.current.id;
            button.tabEnabled = false;
         }
      }
      
      private function doLocaleSwitch(event:Event) : void
      {
         Logger.LogToChannel(Logger.LOCALIZATION,this.helper[event.target]);
         if(!Base.isScaleform)
         {
            trace(this.helper[event.target]);
            this.Locale_current_id = this.helper[event.target];
            this.actualize();
         }
         else
         {
            Locale.setLocaleById(this.helper[event.target]);
         }
      }
   }
}

