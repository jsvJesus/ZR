package com
{
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import scaleform.clik.core.UIComponent;
   
   public class Message extends UIComponent
   {
      protected var OK:Function = new Function();
      
      protected var Cancel:Function = new Function();
      
      internal var messageBoard:Sprite = new Sprite();
      
      internal var MSG:MessageBox = new MessageBox();
      
      public function Message()
      {
         super();
      }
      
      public function onRes(e:Event) : *
      {
         this.x = (Object(root).width - stage.stageWidth) / 2;
         this.y = (Object(root).height - stage.stageHeight) / 2;
         this.MSG.x = ((Object(root).width - stage.stageWidth) / 2 + stage.stageWidth) / 2 - this.MSG.width / 2;
         this.MSG.y = ((Object(root).height - stage.stageHeight) / 2 + stage.stageHeight) / 2 - this.MSG.height / 2;
         this.messageBoard.x = 0;
         this.messageBoard.y = 0;
         this.messageBoard.width = stage.stageWidth + stage.stageWidth / 2;
         this.messageBoard.height = stage.stageHeight + stage.stageHeight / 2;
      }
      
      public function Message1(messageTitle:String, messageText:String, funcOK:Function, funcCancel:Function, OKtext:String, NOtext:String) : *
      {
         stage.addEventListener(Event.RESIZE,this.onRes);
         this.OK = function():*
         {
            funcOK();
         };
         this.Cancel = function():*
         {
            funcCancel();
         };
         this.MSG.Title.text = messageTitle;
         this.MSG.MsgText.htmlText = messageText;
         this.MSG.YesBtn.addEventListener(MouseEvent.CLICK,function():*
         {
            stage.removeEventListener(Event.RESIZE,onRes);
            stage.removeEventListener(KeyboardEvent.KEY_DOWN,keyDownListener);
            funcOK();
         });
         this.MSG.YesBtn.label = OKtext;
         this.MSG.NoBtn.addEventListener(MouseEvent.CLICK,function():*
         {
            stage.removeEventListener(Event.RESIZE,onRes);
            stage.removeEventListener(KeyboardEvent.KEY_DOWN,keyDownListener);
            funcCancel();
         });
         this.MSG.NoBtn.label = NOtext;
         stage.addEventListener(KeyboardEvent.KEY_DOWN,this.keyDownListener);
         this.messageBoard.graphics.beginFill(0,0.1);
         this.messageBoard.graphics.drawRect((Object(root).width - stage.stageWidth) / 2,(Object(root).height - stage.stageHeight) / 2,stage.stageWidth,(Object(root).height + stage.stageHeight) / 2);
         this.messageBoard.graphics.endFill();
         addChild(this.messageBoard);
         addChild(this.MSG);
         this.MSG.x = ((Object(root).width - stage.stageWidth) / 2 + stage.stageWidth) / 2 - this.MSG.width / 2;
         this.MSG.y = ((Object(root).height - stage.stageHeight) / 2 + stage.stageHeight) / 2 - this.MSG.height / 2;
      }
      
      protected function keyDownListener(e:KeyboardEvent) : *
      {
         if(e.keyCode == 27)
         {
            stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.keyDownListener);
            stage.removeEventListener(Event.RESIZE,this.onRes);
            this.Cancel();
         }
      }
   }
}

