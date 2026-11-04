package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.events.Event;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   import lang.Localization;
   
   [Event(name="resize",type="flash.events.Event")]
   public class Label extends Component
   {
      protected var _autoSize:Boolean = true;
      
      protected var _text:String = "";
      
      protected var _tf:TextField;
      
      protected var _size:Number = -1;
      
      private var _$:String;
      
      public function Label(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, text:String = "", size:Number = -1, _$:String = "")
      {
         this.text = text;
         if(Boolean(_$.length))
         {
            this.$ = _$;
         }
         this._size = size;
         super(parent,xpos,ypos);
      }
      
      public function get $() : String
      {
         return this._$;
      }
      
      public function set $(value:String) : void
      {
         this._$ = value;
         Localization.Add(this);
      }
      
      public function updateLocalized() : void
      {
         trace("updateLocalized",this.$);
         var oldAlpha:Number = this.alpha;
         this.visible = false;
         this.text = Localization.GET(this.$);
         this.visible = true;
      }
      
      override protected function init() : void
      {
         super.init();
         mouseEnabled = false;
         mouseChildren = false;
      }
      
      override protected function addChildren() : void
      {
         _height = 18;
         this._tf = new TextField();
         this._tf.height = _height;
         this._tf.embedFonts = Style.embedFonts;
         this._tf.selectable = false;
         this._tf.mouseEnabled = false;
         this._tf.defaultTextFormat = new TextFormat(Style.fontName,this._size > 0 ? this._size : Style.LABEL_FONT_SIZE,Style.LABEL_TEXT);
         this._tf.text = this._text;
         addChild(this._tf);
         this.draw();
      }
      
      override public function draw() : void
      {
         super.draw();
         this._tf.text = this._text;
         if(this._autoSize)
         {
            this._tf.autoSize = TextFieldAutoSize.LEFT;
            _width = this._tf.width;
            dispatchEvent(new Event(Event.RESIZE));
         }
         else
         {
            this._tf.autoSize = TextFieldAutoSize.NONE;
            this._tf.width = _width;
         }
         _height = this._tf.height = 18;
      }
      
      public function set text(t:String) : void
      {
         this._text = t;
         if(this._text == null)
         {
            this._text = "";
         }
         invalidate();
      }
      
      public function get text() : String
      {
         return this._text;
      }
      
      public function set autoSize(b:Boolean) : void
      {
         this._autoSize = b;
      }
      
      public function get autoSize() : Boolean
      {
         return this._autoSize;
      }
      
      public function get textField() : TextField
      {
         return this._tf;
      }
   }
}

