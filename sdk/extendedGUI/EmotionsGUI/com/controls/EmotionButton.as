package com.controls
{
   import com.events.EmotionEvent;
   import flash.display.MovieClip;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import scaleform.clik.controls.Button;
   import scaleform.clik.data.ListDataEmotion;
   import scaleform.clik.utils.Constraints;
   
   public class EmotionButton extends Button
   {
      protected var _alph:uint = 0;
      
      protected var _key_id:Number = 0;
      
      protected var _index:uint = 0;
      
      protected var _key_bind:String;
      
      protected var _id:uint = 0;
      
      public var key_bind:TextField;
      
      public var emotion_button:TextField;
      
      public var edit_picker:MovieClip;
      
      public var selected_frame:MovieClip;
      
      public function EmotionButton()
      {
         super();
      }
      
      public function set index(value:int) : *
      {
         this._index = value;
      }
      
      public function get index() : int
      {
         return this._index;
      }
      
      public function set check(value:Boolean) : *
      {
      }
      
      public function get check() : Boolean
      {
         return false;
      }
      
      public function set key_name(value:String) : *
      {
         this._key_bind = value;
      }
      
      public function get key_name() : String
      {
         return this._key_bind;
      }
      
      public function set key_id(value:Number) : *
      {
         this._key_id = value;
      }
      
      public function get key_id() : Number
      {
         return this._key_id;
      }
      
      public function set frame(value:Boolean) : *
      {
         this.selected_frame.visible = value;
      }
      
      override protected function updateText() : void
      {
         if(_label != null && this.emotion_button != null)
         {
            this.emotion_button.text = _label;
            this.key_bind.text = this._key_bind;
         }
      }
      
      public function setListData(listData:ListDataEmotion) : void
      {
         this.index = listData.index;
         label = listData.label || "";
         this.key_name = listData.key_name;
         this.key_id = listData.key_id;
         this._id = listData.id;
      }
      
      override protected function initialize() : void
      {
         super.initialize();
      }
      
      override protected function configUI() : void
      {
         if(!constraintsDisabled)
         {
            constraints.addElement("key_bind",this.key_bind,Constraints.RIGHT);
            constraints.addElement("selected_frame",this.selected_frame,Constraints.ALL);
         }
         if(this.emotion_button != null)
         {
            this.emotion_button.autoSize = TextFieldAutoSize.LEFT;
         }
         this.key_bind.autoSize = TextFieldAutoSize.RIGHT;
         addEventListener(MouseEvent.CLICK,this.onClick);
         super.configUI();
      }
      
      protected function onClick(e:MouseEvent) : *
      {
         dispatchEvent(new EmotionEvent(EmotionEvent.CLICK,this,this._id,false,this.index));
      }
      
      protected function onKeyBindClick(e:MouseEvent) : *
      {
      }
   }
}

