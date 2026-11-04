package com.components
{
   import com.communication.Localization;
   import com.controls.Card;
   import com.controls.Cards;
   import com.events.helpEvent;
   import flash.display.Sprite;
   import flash.events.TextEvent;
   import flash.text.TextField;
   import flash.text.TextFormat;
   
   public class Chapter extends Sprite
   {
      protected var _id:Number = -1;
      
      public var _title:String = "";
      
      public var _nomenclature:Array = new Array();
      
      public var _contentsIDs:Array = new Array();
      
      protected var _positions:Object = new Object();
      
      protected var _end_positions:Object = new Object();
      
      protected var _position:Number = 0;
      
      public function Chapter(param1:Number, param2:String, param3:Array, param4:Array)
      {
         super();
         this.id = param1;
         this.title = param2;
         this.nomenclature = param3;
         this.contentsIDs = param4;
      }
      
      public function set id(param1:Number) : *
      {
         this._id = param1;
      }
      
      public function get id() : Number
      {
         return this._id;
      }
      
      public function set title(param1:String) : *
      {
         this._title = param1;
      }
      
      public function get title() : String
      {
         return this._title;
      }
      
      public function set nomenclature(param1:Array) : *
      {
         this._nomenclature = param1;
      }
      
      public function get nomenclature() : Array
      {
         return this._nomenclature;
      }
      
      public function set contentsIDs(param1:Array) : *
      {
         this._contentsIDs = param1;
      }
      
      public function get contentsIDs() : Array
      {
         return this._contentsIDs;
      }
      
      public function DrawCapter(param1:Number) : *
      {
         this._position = 0;
         var _loc2_:* = 0;
         while(_loc2_ < numChildren)
         {
            removeChild(getChildAt(_loc2_));
            _loc2_++;
         }
         this.addTitle();
         this.addNomenclature();
         this.addDiscription(param1);
         this.addContents(param1);
      }
      
      protected function addTitle() : *
      {
         var _loc1_:* = new TextField();
         _loc1_.autoSize = "true";
         var _loc2_:* = new TextFormat();
         _loc2_.font = "Roboto Condensed Bold";
         _loc2_.size = 36;
         _loc2_.color = 14269073;
         _loc1_.defaultTextFormat = _loc2_;
         _loc1_.htmlText = Localization.getLocal(this._title);
         _loc1_.setTextFormat(_loc2_);
         _loc1_.height = _loc1_.textHeight + 6;
         _loc1_.selectable = false;
         addChild(_loc1_);
         this._position += _loc1_.height + 10;
      }
      
      protected function handleSayId(param1:TextEvent) : *
      {
         dispatchEvent(new helpEvent(helpEvent.SCROLL,Number(param1.text)));
      }
      
      protected function addNomenclature() : *
      {
         var _loc1_:* = undefined;
         var _loc2_:TextField = null;
         var _loc3_:* = undefined;
         for(_loc1_ in this._nomenclature)
         {
            _loc2_ = new TextField();
            _loc2_.autoSize = "true";
            _loc2_.htmlText = "<a href=\'event:" + this._nomenclature[_loc1_].id + "\'>" + Localization.getLocal(this._nomenclature[_loc1_].name) + "</a>";
            _loc3_ = new TextFormat();
            _loc3_.font = "Roboto Condensed Regular";
            _loc3_.size = 15;
            _loc3_.color = 16777215;
            _loc2_.background = false;
            _loc2_.border = true;
            _loc3_.indent = 28;
            _loc2_.setTextFormat(_loc3_);
            _loc2_.height = _loc2_.textHeight + 3;
            _loc2_.y = this._position - 4;
            _loc2_.selectable = false;
            this._position += _loc2_.height - 4;
            _loc2_.addEventListener(TextEvent.LINK,this.handleSayId);
            addChild(_loc2_);
         }
      }
      
      protected function addDiscription(param1:Number) : *
      {
         this._position += 20;
         var _loc2_:Card = Cards.getCardById(this._id);
         var _loc3_:FullCard = new FullCard(param1,_loc2_.id,_loc2_.title,_loc2_.full);
         _loc3_.y = this._position;
         this._position += _loc3_.sizeHeight;
         addChild(_loc3_);
      }
      
      protected function addContents(param1:Number) : *
      {
         var _loc2_:* = undefined;
         var _loc3_:Card = null;
         var _loc4_:FullCard = null;
         for(_loc2_ in this._contentsIDs)
         {
            this._position += 20;
            _loc3_ = Cards.getCardById(this._contentsIDs[_loc2_]);
            this._positions[_loc3_.id] = this._position;
            _loc4_ = new FullCard(param1,_loc3_.id,_loc3_.title,_loc3_.full);
            _loc4_.y = this._position;
            this._position += _loc4_.sizeHeight;
            this._end_positions[_loc3_.id] = this._position;
            addChild(_loc4_);
         }
      }
      
      public function getPosition(param1:Number) : Number
      {
         var _loc2_:* = undefined;
         for(_loc2_ in this._positions)
         {
         }
         if(this._positions[param1] != null)
         {
            return this._positions[param1];
         }
         return 0;
      }
      
      public function getEndPosition(param1:Number) : Number
      {
         if(this._end_positions[param1] != null)
         {
            return this._end_positions[param1];
         }
         return 0;
      }
   }
}

