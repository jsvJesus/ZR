package com.dvalimona.components
{
   import flash.display.*;
   import flash.events.*;
   import logging.*;
   
   public class RadioButton extends Component
   {
      protected static var buttons:Array;
      
      protected var _back:Sprite;
      
      protected var _button:Sprite;
      
      protected var _selected:Boolean = false;
      
      protected var _label:Label;
      
      protected var _labelText:String = "";
      
      protected var _groupName:String = "defaultRadioGroup";
      
      public function RadioButton(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:String = "", param5:Boolean = false, param6:Function = null)
      {
         var parent:DisplayObjectContainer = param1;
         var xpos:Number = param2;
         var ypos:Number = param3;
         var label:String = param4;
         var checked:Boolean = param5;
         var defaultHandler:Function = param6;
         try
         {
            RadioButton.addButton(this);
            this._selected = checked;
            this._labelText = label;
            super(parent,xpos,ypos);
            if(defaultHandler != null)
            {
               addEventListener(MouseEvent.CLICK,defaultHandler);
            }
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
      }
      
      protected static function addButton(param1:RadioButton) : void
      {
         var rb:RadioButton = param1;
         try
         {
            if(buttons == null)
            {
               buttons = new Array();
            }
            buttons.push(rb);
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
      }
      
      protected static function clear(param1:RadioButton) : void
      {
         var i:uint = 0;
         var rb:RadioButton = param1;
         try
         {
            i = 0;
            while(i < buttons.length)
            {
               if(buttons[i] != rb && buttons[i].groupName == rb.groupName)
               {
                  buttons[i].selected = false;
               }
               i++;
            }
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
      }
      
      override protected function init() : void
      {
         try
         {
            super.init();
            buttonMode = true;
            useHandCursor = true;
            addEventListener(MouseEvent.CLICK,this.onClick,false,1);
            this.selected = this._selected;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
      }
      
      override protected function addChildren() : void
      {
         try
         {
            this._back = new Sprite();
            addChild(this._back);
            this._button = new Sprite();
            this._button.visible = false;
            addChild(this._button);
            this._label = new Label(this,0,0,this._labelText);
            this._label.size = 22;
            this.draw();
            mouseChildren = false;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
      }
      
      override public function draw() : void
      {
         try
         {
            super.draw();
            this._back.graphics.clear();
            this._back.graphics.beginFill(Style.BACKGROUND);
            this._back.graphics.drawCircle(5,5,5);
            this._back.graphics.endFill();
            this._button.graphics.clear();
            this._button.graphics.beginFill(Style.BUTTON_UP);
            this._button.graphics.drawCircle(5,5,3);
            this._label.x = 12;
            this._label.y = (10 - this._label.height) / 2;
            this._label.text = this._labelText;
            this._label.draw();
            _width = this._label.width + 12;
            _height = 10;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
      }
      
      protected function onClick(param1:MouseEvent) : void
      {
         var event:MouseEvent = param1;
         try
         {
            this.selected = true;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
      }
      
      public function set selected(param1:Boolean) : void
      {
         var s:Boolean = param1;
         try
         {
            this._selected = s;
            this._button.visible = this._selected;
            if(this._selected)
            {
               RadioButton.clear(this);
            }
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
      }
      
      public function get selected() : Boolean
      {
         try
         {
            return this._selected;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
         return false;
      }
      
      public function set label(param1:String) : void
      {
         var str:String = param1;
         try
         {
            this._labelText = str;
            invalidate();
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
      }
      
      public function get label() : String
      {
         try
         {
            return this._labelText;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
         return "761982763";
      }
      
      public function get groupName() : String
      {
         return this._groupName;
      }
      
      public function set groupName(param1:String) : void
      {
         var value:String = param1;
         try
         {
            this._groupName = value;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharButton.updateContent error:",error);
         }
      }
   }
}

