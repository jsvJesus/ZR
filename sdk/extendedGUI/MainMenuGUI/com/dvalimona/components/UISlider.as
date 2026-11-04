package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   
   public class UISlider extends Component
   {
      protected var _label:Label;
      
      protected var _valueLabel:Label;
      
      protected var _slider:Slider;
      
      protected var _precision:int = 1;
      
      protected var _sliderClass:Class;
      
      protected var _labelText:String;
      
      protected var _tick:Number = 1;
      
      private var _defaultValue:Number;
      
      private var _initValue:Number;
      
      public function UISlider(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, label:String = "", defaultHandler:Function = null)
      {
         this._labelText = label;
         super(parent,xpos,ypos);
         if(defaultHandler != null)
         {
            addEventListener(Event.CHANGE,defaultHandler);
         }
         this.formatValueLabel();
      }
      
      public function get defaultValue() : Number
      {
         return this._defaultValue;
      }
      
      public function set defaultValue(newValue:Number) : void
      {
         this._defaultValue = this.preciseValue(newValue);
      }
      
      public function set initValue(newValue:Number) : void
      {
         this._initValue = this.preciseValue(newValue);
      }
      
      public function get initValue() : Number
      {
         return this._initValue;
      }
      
      public function get isDefaults() : Boolean
      {
         var mult:Number = Math.pow(10,this._precision);
         return Math.round(this.defaultValue * mult) == Math.round(this._slider.value * mult);
      }
      
      public function get changed() : Boolean
      {
         var mult:Number = Math.pow(10,this._precision);
         return Math.round(this.initValue * mult) != Math.round(this._slider.value * mult);
      }
      
      override protected function addChildren() : void
      {
         this._label = new Label(this,0,0);
         this._slider = new this._sliderClass(this,0,0,this.onSliderChange);
         this._valueLabel = new Label(this);
         this._valueLabel.size = Style.SLIDER_LABEL_SIZE;
         this._valueLabel.align = Label.CENTER;
         this._valueLabel.color = Style.SLIDER_LABEL_COLOR;
         this._valueLabel.autoSize = false;
         this._valueLabel.debug = false;
         this._valueLabel.width = Style.SLIDER_HANDLE_WIDTH;
      }
      
      protected function formatValueLabel() : void
      {
         var i:uint = 0;
         if(isNaN(this._slider.value))
         {
            this.getLabel().text = "NaN";
            this.positionLabel();
            return;
         }
         var val:* = String(this.preciseValue(this._slider.value));
         var parts:Array = val.split(".");
         if(parts[1] == null)
         {
            if(this._precision > 0)
            {
               val += ".";
            }
            for(i = 0; i < this._precision; i++)
            {
               val += "0";
            }
         }
         else if(parts[1].length < this._precision)
         {
            for(i = 0; i < this._precision - parts[1].length; i++)
            {
               val += "0";
            }
         }
         this.getLabel().text = val;
         this.positionLabel();
      }
      
      protected function getLabel() : Object
      {
         return this._valueLabel;
      }
      
      protected function preciseValue(valueToPrecise:Number) : Number
      {
         var mult:Number = Math.pow(10,this._precision);
         return Math.round(valueToPrecise * mult) / mult;
      }
      
      protected function positionLabel() : void
      {
      }
      
      override public function draw() : void
      {
         super.draw();
         this._label.text = this._labelText;
         this._label.draw();
         this.formatValueLabel();
      }
      
      public function setSliderParams(min:Number, max:Number, value:Number) : void
      {
         this._slider.setSliderParams(min,max,value);
      }
      
      protected function onSliderChange(event:Event) : void
      {
         this.formatValueLabel();
         this.positionLabel();
         dispatchEvent(new Event(Event.CHANGE));
      }
      
      public function set value(v:Number) : void
      {
         this._slider.value = v;
         this.formatValueLabel();
         invalidate();
      }
      
      public function get value() : Number
      {
         return this._slider.value;
      }
      
      public function set maximum(m:Number) : void
      {
         this._slider.maximum = m;
         invalidate();
      }
      
      public function get maximum() : Number
      {
         return this._slider.maximum;
      }
      
      public function set minimum(m:Number) : void
      {
         this._slider.minimum = m;
         invalidate();
      }
      
      public function get minimum() : Number
      {
         return this._slider.minimum;
      }
      
      public function set labelPrecision(decimals:int) : void
      {
         this._precision = decimals;
         invalidate();
      }
      
      public function get labelPrecision() : int
      {
         return this._precision;
      }
      
      public function set label(str:String) : void
      {
         this._labelText = str;
         this.draw();
      }
      
      public function get label() : String
      {
         return this._labelText;
      }
      
      public function set tick(t:Number) : void
      {
         this._tick = t;
         this._slider.tick = this._tick;
      }
      
      public function get tick() : Number
      {
         return this._tick;
      }
   }
}

