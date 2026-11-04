package com
{
   import flash.display.MovieClip;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import scaleform.clik.core.UIComponent;
   
   public class translate extends UIComponent
   {
      public var CloseBtn:s_closeBtn;
      
      public var EBRUBtn:TranslButton;
      
      public var InputText:TextField;
      
      public var OutputText:TextField;
      
      public var RUENBtn:TranslButton;
      
      public var Scroll:DefaultScrollBar;
      
      public var Scroll2:DefaultScrollBar;
      
      public var Title:TextField;
      
      public var ToChatBtn:TranslButton;
      
      public var background:MovieClip;
      
      internal var API:GameCommunication = new GameCommunication();
      
      public function translate()
      {
         super();
      }
      
      public function createTranslate(text:String = "") : *
      {
         this.InputText.text = text;
         this.OutputText.text = "";
      }
      
      public function setTranslationText(str:String) : *
      {
         this.OutputText.text = str;
      }
      
      override protected function configUI() : void
      {
         this.RUENBtn.label = "RU - EN";
         this.EBRUBtn.label = "EN - RU";
         this.ToChatBtn.label = "TO CHAT";
         this.Title.addEventListener(MouseEvent.MOUSE_DOWN,this.onMessageStartDrag);
         this.CloseBtn.addEventListener(MouseEvent.CLICK,this.onClickClose);
         this.RUENBtn.addEventListener(MouseEvent.CLICK,this.translateRUEN);
         this.EBRUBtn.addEventListener(MouseEvent.CLICK,this.translateENRU);
         this.ToChatBtn.addEventListener(MouseEvent.CLICK,this.toChat);
      }
      
      protected function toChat(e:MouseEvent) : *
      {
         Object(root).MainChat.addWhispMsg(this.OutputText.text);
         parent.removeChild(this);
      }
      
      protected function translateRUEN(e:MouseEvent) : *
      {
         this.API.translate(this.InputText.text,"en");
      }
      
      protected function translateENRU(e:MouseEvent) : *
      {
         this.API.translate(this.InputText.text,"ru");
      }
      
      protected function onClickClose(e:MouseEvent) : *
      {
         parent.removeChild(this);
      }
      
      protected function onMessageStartDrag(e:MouseEvent) : *
      {
         this.startDrag();
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onMessageStopDrag);
      }
      
      protected function onMessageStopDrag(e:MouseEvent) : *
      {
         this.stopDrag();
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onMessageStopDrag);
      }
   }
}

