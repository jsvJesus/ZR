package com.components
{
   import com.communication.Localization;
   import com.controls.DefCheckBox;
   import com.controls.LittleCard;
   import com.greensock.TweenMax;
   import com.greensock.easing.*;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import scaleform.clik.controls.Button;
   import scaleform.clik.core.UIComponent;
   
   public class WidgetCard extends UIComponent
   {
      public var Close_label:TextField;
      
      protected var _title:String;
      
      protected var _text:String;
      
      protected var _cards:Array = new Array();
      
      protected var _currentCard:LittleCard;
      
      protected var _index:Number = -1;
      
      protected var _doNotDisturb:Boolean;
      
      public var Title:TextField;
      
      public var Text:TextField;
      
      public var NextBtn:Button;
      
      public var PrevBtn:Button;
      
      public var closeBtn:Button;
      
      public var back:MovieClip;
      
      public var doNotDisturb:DefCheckBox;
      
      protected var _newHeight:Number;
      
      public function WidgetCard()
      {
         super();
         this.NextBtn.visible = false;
         this.NextBtn.addEventListener(MouseEvent.CLICK,this.NextCard);
         this.PrevBtn.visible = false;
         this.PrevBtn.addEventListener(MouseEvent.CLICK,this.PrevCard);
         this.closeBtn.addEventListener(MouseEvent.CLICK,this.CloseCard);
         this.doNotDisturb.addEventListener(Event.SELECT,this.changeDisturb);
      }
      
      public function onGetLocal() : void
      {
         this.Close_label.htmlText = Localization.getLocal("CLOSE_WIDGET");
         this.doNotDisturb.label = Localization.getLocal("DONT_DISTURB");
      }
      
      public function set title(param1:String) : void
      {
         this.Title.htmlText = Localization.getLocal(param1);
      }
      
      public function get title() : String
      {
         return this.Title.text;
      }
      
      public function set currentIndex(param1:Number) : void
      {
         this._index = param1;
         this._cards[param1].see = true;
         this.currentCard = this._cards[param1];
      }
      
      public function get currentIndex() : Number
      {
         return this._index;
      }
      
      public function set currentCard(param1:LittleCard) : void
      {
         param1.see = true;
         this.title = param1.title;
         this.Text.htmlText = param1.text;
         this._newHeight = this.Text.textHeight + 6 - this.Text.height;
         this.Text.height = this.Text.textHeight + 6;
         this.closeBtn.y += this._newHeight;
         this.doNotDisturb.y += this._newHeight;
         this.Close_label.y += this._newHeight;
         this.back.height += this._newHeight;
      }
      
      protected function NextCard(param1:MouseEvent) : *
      {
         this.currentIndex += 1;
         if(this.currentIndex == 0)
         {
            this.PrevBtn.visible = false;
         }
         else
         {
            this.PrevBtn.visible = true;
         }
         if(this.currentIndex < this._cards.length - 1)
         {
            this.NextBtn.visible = true;
         }
         else
         {
            this.NextBtn.visible = false;
         }
      }
      
      protected function PrevCard(param1:MouseEvent) : *
      {
         --this.currentIndex;
         if(this.currentIndex == 0)
         {
            this.PrevBtn.visible = false;
         }
         else
         {
            this.PrevBtn.visible = true;
         }
         if(this.currentIndex < this._cards.length - 1)
         {
            this.NextBtn.visible = true;
         }
         else
         {
            this.NextBtn.visible = false;
         }
      }
      
      protected function CloseCard(param1:MouseEvent) : *
      {
         this.Hide();
      }
      
      protected function changeDisturb(param1:Event) : *
      {
         this._doNotDisturb = param1.target.selected;
      }
      
      public function Show() : *
      {
         if(!this._doNotDisturb)
         {
            TweenMax.to(this,0.6,{
               "alpha":1,
               "y":this.y + this.height,
               "delay":0.01,
               "ease":Expo.easeIn
            });
            this.visible = true;
         }
         if(this.currentIndex == 0)
         {
            this.PrevBtn.visible = false;
         }
         else
         {
            this.PrevBtn.visible = true;
         }
         if(this.currentIndex < this._cards.length - 1)
         {
            this.NextBtn.visible = true;
         }
         else
         {
            this.NextBtn.visible = false;
         }
         if(this._cards.length == 1)
         {
            this.PrevBtn.visible = false;
            this.NextBtn.visible = false;
         }
      }
      
      public function Hide() : *
      {
         TweenMax.to(this,0.6,{
            "alpha":0,
            "y":this.y - this.height,
            "delay":0.01,
            "ease":Expo.easeOut,
            "onComplete":this.onEndHide
         });
      }
      
      public function onEndHide() : *
      {
         this.visible = false;
         this.validateCards();
      }
      
      protected function validateCards() : *
      {
         var _loc1_:* = this._cards.length - 1;
         while(_loc1_ >= 0)
         {
            if(this._cards[_loc1_].see)
            {
               this._cards.splice(_loc1_,1);
            }
            _loc1_--;
         }
         if(this._cards.length > 0)
         {
            this.currentIndex = 0;
            this.Show();
         }
      }
      
      public function addCard(param1:LittleCard) : *
      {
         this._cards.push(param1);
         if(this._cards.length > 1)
         {
            this.NextBtn.visible = true;
         }
         else
         {
            this.currentIndex = 0;
            this.Show();
         }
      }
      
      public function removeCard(param1:Number) : *
      {
         this._cards.splice(param1,1);
      }
   }
}

