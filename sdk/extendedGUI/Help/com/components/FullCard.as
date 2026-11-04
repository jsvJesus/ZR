package com.components
{
   import com.communication.ImageLoader;
   import com.communication.Localization;
   import flash.display.Bitmap;
   import flash.display.Sprite;
   import flash.events.TextEvent;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   
   public class FullCard extends Sprite
   {
      protected var _title:String;
      
      protected var _id:Number;
      
      protected var Position:Number = 0;
      
      protected var CardWidth:Number = 0;
      
      public function FullCard(param1:Number, param2:Number, param3:String, param4:Array)
      {
         super();
         this.title = param3;
         this.id = param2;
         this.CardWidth = param1;
         this.parseContent(param4);
      }
      
      public function get sizeHeight() : Number
      {
         return this.Position;
      }
      
      public function set id(param1:Number) : void
      {
         this._id = param1;
      }
      
      public function get id() : Number
      {
         return this._id;
      }
      
      public function set title(param1:String) : void
      {
         this._title = param1;
      }
      
      public function get title() : String
      {
         return this._title;
      }
      
      protected function addParagraph(param1:Object) : *
      {
         switch(param1.type)
         {
            case "title":
               this.addTitle(param1.value);
               break;
            case "subtitle":
               this.addSubTitle(param1.value);
               break;
            case "image":
               this.addImage(param1.value);
               break;
            case "text":
               this.addText(param1.value);
               break;
            case "pre":
               this.addPre(param1.value);
         }
      }
      
      protected function addSubTitle(param1:String) : *
      {
         var _loc2_:TextFormat = new TextFormat();
         _loc2_.font = "Roboto Condensed Bold";
         _loc2_.size = 20;
         _loc2_.indent = 28;
         _loc2_.color = 14269073;
         var _loc3_:TextField = new TextField();
         _loc3_.defaultTextFormat = _loc2_;
         _loc3_.htmlText = Localization.getLocal(param1);
         _loc3_.background = true;
         _loc3_.multiline = true;
         _loc3_.setTextFormat(_loc2_);
         _loc3_.backgroundColor = 5197643;
         _loc3_.multiline = true;
         _loc3_.y = this.Position;
         _loc3_.width = this.CardWidth;
         _loc3_.height = _loc3_.textHeight + 6;
         _loc3_.selectable = false;
         _loc3_.wordWrap = true;
         addChild(_loc3_);
         this.Position += _loc3_.height;
      }
      
      protected function addTitle(param1:String) : *
      {
         var _loc2_:TextFormat = new TextFormat();
         _loc2_.font = "Roboto Condensed Bold";
         _loc2_.size = 26;
         _loc2_.indent = 14;
         _loc2_.color = 14269073;
         var _loc3_:TextField = new TextField();
         _loc3_.defaultTextFormat = _loc2_;
         _loc3_.htmlText = Localization.getLocal(param1);
         _loc3_.background = true;
         _loc3_.multiline = true;
         _loc3_.setTextFormat(_loc2_);
         _loc3_.backgroundColor = 2697511;
         _loc3_.multiline = true;
         _loc3_.y = this.Position;
         _loc3_.width = this.CardWidth;
         _loc3_.height = _loc3_.textHeight + 6;
         _loc3_.selectable = false;
         _loc3_.wordWrap = true;
         addChild(_loc3_);
         this.Position += _loc3_.height;
      }
      
      protected function addImage(param1:String) : *
      {
         this.Position += 20;
         var _loc2_:Bitmap = ImageLoader.getImageByName(param1);
         var _loc3_:Number = 0;
         _loc2_.y = this.Position;
         if(_loc2_.width > this.CardWidth)
         {
            _loc3_ = _loc2_.height / _loc2_.width;
            _loc2_.width = this.CardWidth;
            _loc2_.height = this.CardWidth * _loc3_;
         }
         _loc2_.x = (this.CardWidth - _loc2_.width) / 2;
         addChild(_loc2_);
         this.Position += _loc2_.height + 20;
      }
      
      protected function addText(param1:String) : *
      {
         var _loc2_:TextField = new TextField();
         var _loc3_:TextFormat = new TextFormat();
         _loc3_.font = "Roboto Condensed Regular";
         _loc3_.size = 15;
         _loc3_.color = 16777215;
         _loc2_.multiline = true;
         _loc2_.wordWrap = true;
         _loc2_.htmlText = Localization.getLocal(param1);
         _loc2_.setTextFormat(_loc3_);
         _loc2_.x = 28;
         _loc2_.y = this.Position;
         _loc2_.width = this.CardWidth - _loc2_.x;
         _loc2_.height = (Math.round(_loc2_.textWidth / _loc2_.width) + 1) * (_loc2_.textHeight + 6);
         _loc2_.selectable = false;
         _loc2_.addEventListener(TextEvent.LINK,this.onClickLink);
         addChild(_loc2_);
         this.Position += _loc2_.height;
      }
      
      protected function addPre(param1:String) : *
      {
         var _loc2_:TextField = null;
         _loc2_ = new TextField();
         var _loc3_:TextFormat = new TextFormat();
         _loc3_.font = "Roboto Condensed Regular";
         _loc3_.size = 15;
         _loc3_.italic = true;
         _loc3_.leftMargin = 15;
         _loc3_.rightMargin = 15;
         _loc3_.color = 16777215;
         _loc2_.multiline = true;
         _loc2_.wordWrap = true;
         _loc2_.htmlText = Localization.getLocal(param1);
         _loc2_.setTextFormat(_loc3_);
         _loc2_.border = true;
         _loc2_.autoSize = TextFieldAutoSize.CENTER;
         _loc2_.background = true;
         _loc2_.x = 40;
         _loc2_.y = this.Position;
         _loc2_.width = this.CardWidth - _loc2_.x;
         _loc2_.height = (Math.round(_loc2_.textWidth / _loc2_.width) + 1) * (_loc2_.textHeight + 6) + 20;
         _loc2_.width = Math.min(_loc2_.width,_loc2_.textWidth + 21);
         _loc2_.selectable = false;
         _loc2_.addEventListener(TextEvent.LINK,this.onClickLink);
         addChild(_loc2_);
         this.Position += _loc2_.height;
      }
      
      protected function parseContent(param1:Array) : *
      {
         var _loc2_:* = undefined;
         if(this.title != "")
         {
            this.addParagraph({
               "type":"title",
               "value":this.title
            });
         }
         for(_loc2_ in param1)
         {
            this.addParagraph(param1[_loc2_]);
         }
      }
      
      public function onClickLink(param1:TextEvent) : *
      {
         Object(root).show_paragraph(Number(param1.text));
      }
   }
}

