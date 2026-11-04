package com.controls
{
   import com.communication.Localization;
   import com.events.helpEvent;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import scaleform.clik.constants.ConstrainMode;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.controls.Button;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.utils.Constraints;
   
   public class helpListItem extends UIComponent
   {
      protected var _DataArray:Array = new Array();
      
      protected var _index:Number = -1;
      
      protected var _id:Number = -1;
      
      protected var _selected:Boolean;
      
      protected var List:Sprite = new Sprite();
      
      protected var _foreground:Sprite = new Sprite();
      
      public var listItem:CheckBox;
      
      public var itemRendererName:String = "ButtonList";
      
      public function helpListItem()
      {
         super();
         this.listItem.addEventListener(Event.SELECT,this.handleClick);
      }
      
      override protected function preInitialize() : void
      {
         constraints = new Constraints(this,ConstrainMode.REFLOW);
         this._foreground.graphics.beginFill(0);
         this._foreground.x = -1;
         this._foreground.y = 7;
         this._foreground.visible = false;
         this._foreground.graphics.drawRect(0,0,15,15);
         addChild(this._foreground);
      }
      
      public function set id(param1:Number) : void
      {
         this._id = param1;
      }
      
      public function get id() : Number
      {
         return this._id;
      }
      
      public function set index(param1:Number) : void
      {
         this._index = param1;
         this.listItem.index = param1;
      }
      
      public function get index() : Number
      {
         return this._index;
      }
      
      public function set unselected(param1:Boolean) : void
      {
         this.listItem.unselected = param1;
         this.listItem.invalidateState();
      }
      
      public function set data(param1:Array) : void
      {
         this._DataArray = param1;
         this._foreground.visible = this._DataArray.length == 0;
      }
      
      public function get data() : Array
      {
         return this._DataArray;
      }
      
      public function set label(param1:String) : void
      {
         this.listItem.label = Localization.getLocal(param1);
      }
      
      protected function handleClick(param1:Event) : *
      {
         if(!this.listItem.selected)
         {
            this.hideSecList();
            this._selected = false;
         }
         else
         {
            this.showSecList();
            this._selected = true;
         }
         dispatchEvent(new helpEvent(helpEvent.SELECT,0,this._id));
      }
      
      protected function onClickItem(param1:MouseEvent) : *
      {
         if(param1.target is Button)
         {
            dispatchEvent(new helpEvent(helpEvent.SELECT,0,param1.target.index));
         }
      }
      
      protected function showSecList() : *
      {
         var _loc1_:* = undefined;
         var _loc2_:ButtonList = null;
         for(_loc1_ in this.data)
         {
            _loc2_ = new ButtonList();
            _loc2_.y = this.height;
            this.height += _loc2_.height;
            _loc2_.label = Localization.getLocal(this.data[_loc1_].name);
            _loc2_.index = this.data[_loc1_].id;
            _loc2_.addEventListener(MouseEvent.CLICK,this.onClickItem);
            this.List.addChild(_loc2_);
         }
         addChild(this.List);
      }
      
      protected function hideSecList() : *
      {
         while(this.List.numChildren > 0)
         {
            this.List.removeChildAt(0);
         }
         removeChild(this.List);
      }
      
      override protected function draw() : void
      {
         if(isInvalid(InvalidationType.SIZE))
         {
         }
      }
   }
}

