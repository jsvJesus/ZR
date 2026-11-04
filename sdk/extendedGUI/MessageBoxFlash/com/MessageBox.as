package com
{
   import com.greensock.TweenMax;
   import com.greensock.easing.*;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.FocusEvent;
   import flash.events.MouseEvent;
   import flash.events.TextEvent;
   import flash.external.ExternalInterface;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.utils.setTimeout;
   
   public class MessageBox extends MovieClip
   {
      public var Title:TextField;
      
      public var MsgText:TextField;
      
      public var background:MovieClip;
      
      public var inputBox:TextField;
      
      public var buttons:Object = new Object();
      
      protected var _msgName:String = "";
      
      protected var oldInputString:String = "";
      
      protected var newInputString:String = "";
      
      protected var input_len:int = 0;
      
      public function MessageBox()
      {
         super();
         this.z = 500;
         this.inputBox.backgroundColor = 2631720;
         this.inputBox.visible = false;
         this.MsgText.autoSize = TextFieldAutoSize.CENTER;
         this.MsgText.wordWrap = true;
      }
      
      public function get msgName() : String
      {
         return this._msgName;
      }
      
      public function set msgName(text:String) : *
      {
         this._msgName = text;
         this.name = text;
      }
      
      public function set titleText(text:String) : *
      {
         this.Title.htmlText = text;
      }
      
      public function get titleText() : String
      {
         return this.Title.text;
      }
      
      public function setParam(name:String, title:String, text:String, input:Boolean, buttons:Array, buttonsName:Object, def:String, active:Boolean) : *
      {
         this.msgName = name;
         this.titleText = title;
         this.inputBox.visible = input;
         if(this.inputBox.visible)
         {
            this.inputBox.htmlText = def;
            this.inputBox.addEventListener(TextEvent.TEXT_INPUT,this.beforeInput);
            this.inputBox.addEventListener(Event.CHANGE,this.afterInput);
            this.inputBox.addEventListener(FocusEvent.FOCUS_IN,this.onFocusIn);
            this.inputBox.addEventListener(FocusEvent.FOCUS_OUT,this.onFocusOut);
            if(active)
            {
               stage.focus = this.inputBox;
            }
         }
         this.MsgText.htmlText = text;
         this.drawBackground();
         this.drawButtons(buttons,buttonsName);
      }
      
      protected function drawBackground() : *
      {
         var _h:Number = 0;
         if(this.inputBox.visible)
         {
            if(this.MsgText.text == "")
            {
               this.inputBox.y = this.MsgText.y;
            }
            else
            {
               this.inputBox.y = this.MsgText.y + this.MsgText.height;
            }
            _h = this.inputBox.y + this.inputBox.height + 10;
         }
         else
         {
            _h = this.MsgText.y + this.MsgText.height + 10;
         }
         this.background.height = Math.max(_h,this.background.height);
      }
      
      protected function drawButtons(buttons:Array, buttonsNames:Object) : *
      {
         var btn:String = null;
         var text:String = null;
         var _w:Number = this.width / buttons.length + 1 / buttons.length;
         var _x:Number = 0;
         for(var i:int = 0; i < buttons.length; i++)
         {
            btn = buttons[i];
            text = "";
            if(buttonsNames[btn])
            {
               text = buttonsNames[btn];
            }
            else if(MessageConstans.Local[btn])
            {
               text = MessageConstans.Local[btn];
            }
            else
            {
               text = btn;
            }
            this.buttons[btn] = new SOButton();
            this.buttons[btn].label = text;
            this.buttons[btn].y = this.background.height + 1;
            this.buttons[btn].x = _x;
            this.buttons[btn].width = _w - 1;
            this.buttons[btn].name = btn;
            this.buttons[btn].addEventListener(MouseEvent.CLICK,this.onClickBtn);
            _x = _w * (i + 1);
            addChild(this.buttons[btn]);
         }
      }
      
      protected function blinkError() : *
      {
         this.inputBox.backgroundColor = 10027008;
         setTimeout(function():*
         {
            inputBox.backgroundColor = 2631720;
         },1500);
      }
      
      protected function onClickBtn(e:MouseEvent) : *
      {
         var data:* = new Object();
         data.name = this.msgName;
         if(MessageConstans.BUTTON_EV[e.target.name] != null)
         {
            data.btn = MessageConstans.BUTTON_EV[e.target.name];
         }
         else
         {
            data.btn = e.target.name;
         }
         data.input = this.inputBox.visible;
         if(this.inputBox.visible)
         {
            data.input_text = this.inputBox.text;
         }
         else
         {
            data.input_text = "";
         }
         data.event = MessageConstans.EVENT_BTNPRESS;
         ExternalInterface.call("on_click_btn",data);
         this.hide();
      }
      
      public function show() : *
      {
         TweenMax.to(this,1,{
            "delay":0,
            "z":0,
            "ease":Expo.easeOut
         });
      }
      
      public function hide() : *
      {
         TweenMax.to(this,1,{
            "alpha":0,
            "delay":0,
            "z":500,
            "onComplete":this.destroy,
            "ease":Expo.easeOut
         });
      }
      
      protected function destroy() : *
      {
         if(this.parent)
         {
            this.parent.removeChild(this);
         }
      }
      
      protected function beforeInput(e:TextEvent) : *
      {
         this.oldInputString = this.inputBox.text;
         this.input_len = e.text.length;
         this.newInputString = this.oldInputString.substr(0,this.inputBox.caretIndex) + e.text + this.oldInputString.substr(this.inputBox.caretIndex);
         this.inputBox.restrict = "";
         var obj:Object = new Object();
         obj.data = new Object();
         obj.data.id = this.msgName;
         obj.data.newchar = e.text;
         obj.data.text = this.oldInputString;
         obj.data.offset = this.inputBox.caretIndex;
         ExternalInterface.call("validate_input",obj);
      }
      
      protected function afterInput(e:Event) : *
      {
         this.inputBox.restrict = null;
      }
      
      protected function onFocusIn(e:FocusEvent) : *
      {
         var obj:Object = new Object();
         obj.now_input = true;
         ExternalInterface.call("onInputFocus",obj);
      }
      
      protected function onFocusOut(e:FocusEvent) : *
      {
         var obj:Object = new Object();
         obj.now_input = false;
         ExternalInterface.call("onInputFocus",obj);
      }
      
      public function validateInput(check:Boolean) : *
      {
         if(check)
         {
            this.inputBox.text = this.newInputString;
            this.inputBox.setSelection(this.inputBox.caretIndex + this.input_len,this.inputBox.caretIndex + this.input_len);
         }
         else
         {
            this.blinkError();
            this.inputBox.text = this.oldInputString;
         }
      }
   }
}

