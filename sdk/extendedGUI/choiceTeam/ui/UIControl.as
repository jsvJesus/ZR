package ui
{
   import flash.display.DisplayObject;
   import flash.display.Sprite;
   import flash.errors.IllegalOperationError;
   import flash.events.Event;
   import flash.geom.Matrix;
   import flash.geom.Point;
   
   public class UIControl extends Sprite
   {
      private static const HELPER_MATRIX:Matrix = new Matrix();
      
      private static const HELPER_POINT:Point = new Point();
      
      public static const CREATION_COMPLETE:String = "creation-complete";
      
      public static const INITIALIZE:String = "initialize-ui";
      
      public static const FOCUS_IN:String = "focus-in";
      
      public static const FOCUS_OUT:String = "focus-out";
      
      public static const INVALIDATION_FLAG_ALL:String = "all";
      
      public static const INVALIDATION_FLAG_STATE:String = "state";
      
      public static const INVALIDATION_FLAG_SIZE:String = "size";
      
      public static const INVALIDATION_FLAG_STYLES:String = "styles";
      
      public static const INVALIDATION_FLAG_SKIN:String = "skin";
      
      public static const INVALIDATION_FLAG_LAYOUT:String = "layout";
      
      public static const INVALIDATION_FLAG_DATA:String = "data";
      
      public static const INVALIDATION_FLAG_SCROLL:String = "scroll";
      
      public static const INVALIDATION_FLAG_SELECTED:String = "selected";
      
      public static const INVALIDATION_FLAG_FOCUS:String = "focus";
      
      protected static const INVALIDATION_FLAG_TEXT_RENDERER:String = "textRenderer";
      
      protected static const INVALIDATION_FLAG_TEXT_EDITOR:String = "textEditor";
      
      protected static const ILLEGAL_WIDTH_ERROR:String = "A component\'s width cannot be NaN.";
      
      protected static const ILLEGAL_HEIGHT_ERROR:String = "A component\'s height cannot be NaN.";
      
      protected static const ABSTRACT_CLASS_ERROR:String = "FeathersControl is an abstract class. For a lightweight Feathers wrapper, use feathers.controls.LayoutGroup.";
      
      protected var _isInitialized:Boolean = false;
      
      protected var _isAllInvalid:Boolean = false;
      
      protected var _invalidationFlags:Object = {};
      
      protected var _delayedInvalidationFlags:Object = {};
      
      protected var _isEnabled:Boolean = true;
      
      protected var explicitWidth:Number = NaN;
      
      protected var actualWidth:Number = 0;
      
      protected var scaledActualWidth:Number = 0;
      
      protected var explicitHeight:Number = NaN;
      
      protected var actualHeight:Number = 0;
      
      protected var scaledActualHeight:Number = 0;
      
      protected var _minWidth:Number = 0;
      
      protected var _minHeight:Number = 0;
      
      protected var _maxWidth:Number = Infinity;
      
      protected var _maxHeight:Number = Infinity;
      
      protected var _focusManager:IFocusManager;
      
      protected var _focusOwner:IFocusDisplayObject;
      
      protected var _isFocusEnabled:Boolean = true;
      
      protected var _nextTabFocus:IFocusDisplayObject;
      
      protected var _previousTabFocus:IFocusDisplayObject;
      
      protected var _focusIndicatorSkin:DisplayObject;
      
      protected var _focusPaddingTop:Number = 0;
      
      protected var _focusPaddingRight:Number = 0;
      
      protected var _focusPaddingBottom:Number = 0;
      
      protected var _focusPaddingLeft:Number = 0;
      
      protected var _hasFocus:Boolean = false;
      
      protected var _showFocus:Boolean = false;
      
      protected var _isValidating:Boolean = false;
      
      protected var _hasValidated:Boolean = false;
      
      protected var _depth:int = -1;
      
      protected var _invalidateCount:int = 0;
      
      protected var _isDisposed:Boolean = false;
      
      public function UIControl()
      {
         super();
      }
      
      public function get isInitialized() : Boolean
      {
         return this._isInitialized;
      }
      
      override public function get width() : Number
      {
         return this.scaledActualWidth;
      }
      
      override public function set width(value:Number) : void
      {
         if(this.explicitWidth == value)
         {
            return;
         }
         var valueIsNaN:Boolean = value !== value;
         if(valueIsNaN && this.explicitWidth !== this.explicitWidth)
         {
            return;
         }
         this.explicitWidth = value;
         if(valueIsNaN)
         {
            this.actualWidth = this.scaledActualWidth = 0;
            this.invalidate(INVALIDATION_FLAG_SIZE);
         }
         else
         {
            this.setSizeInternal(value,this.actualHeight,true);
         }
      }
      
      override public function get height() : Number
      {
         return this.scaledActualHeight;
      }
      
      override public function set height(value:Number) : void
      {
         if(this.explicitHeight == value)
         {
            return;
         }
         var valueIsNaN:Boolean = value !== value;
         if(valueIsNaN && this.explicitHeight !== this.explicitHeight)
         {
            return;
         }
         this.explicitHeight = value;
         if(valueIsNaN)
         {
            this.actualHeight = this.scaledActualHeight = 0;
            this.invalidate(INVALIDATION_FLAG_SIZE);
         }
         else
         {
            this.setSizeInternal(this.actualWidth,value,true);
         }
      }
      
      public function get minWidth() : Number
      {
         return this._minWidth;
      }
      
      public function set minWidth(value:Number) : void
      {
         if(this._minWidth == value)
         {
            return;
         }
         if(value !== value)
         {
            throw new ArgumentError("minWidth cannot be NaN");
         }
         this._minWidth = value;
         this.invalidate(INVALIDATION_FLAG_SIZE);
      }
      
      public function get minHeight() : Number
      {
         return this._minHeight;
      }
      
      public function set minHeight(value:Number) : void
      {
         if(this._minHeight == value)
         {
            return;
         }
         if(value !== value)
         {
            throw new ArgumentError("minHeight cannot be NaN");
         }
         this._minHeight = value;
         this.invalidate(INVALIDATION_FLAG_SIZE);
      }
      
      public function get maxWidth() : Number
      {
         return this._maxWidth;
      }
      
      public function set maxWidth(value:Number) : void
      {
         if(this._maxWidth == value)
         {
            return;
         }
         if(value !== value)
         {
            throw new ArgumentError("maxWidth cannot be NaN");
         }
         this._maxWidth = value;
         this.invalidate(INVALIDATION_FLAG_SIZE);
      }
      
      public function get maxHeight() : Number
      {
         return this._maxHeight;
      }
      
      public function set maxHeight(value:Number) : void
      {
         if(this._maxHeight == value)
         {
            return;
         }
         if(value !== value)
         {
            throw new ArgumentError("maxHeight cannot be NaN");
         }
         this._maxHeight = value;
         this.invalidate(INVALIDATION_FLAG_SIZE);
      }
      
      override public function set scaleX(value:Number) : void
      {
         super.scaleX = value;
         this.setSizeInternal(this.actualWidth,this.actualHeight,false);
      }
      
      override public function set scaleY(value:Number) : void
      {
         super.scaleY = value;
         this.setSizeInternal(this.actualWidth,this.actualHeight,false);
      }
      
      public function get focusManager() : IFocusManager
      {
         return this._focusManager;
      }
      
      public function set focusManager(value:IFocusManager) : void
      {
         if(!(this is IFocusDisplayObject))
         {
            throw new IllegalOperationError("Cannot pass a focus manager to a component that does not implement ui.IFocusDisplayObject");
         }
         if(this._focusManager == value)
         {
            return;
         }
         this._focusManager = value;
         if(Boolean(this._focusManager))
         {
            this.addEventListener(FOCUS_IN,this.focusInHandler);
            this.addEventListener(FOCUS_OUT,this.focusOutHandler);
         }
         else
         {
            this.removeEventListener(FOCUS_IN,this.focusInHandler);
            this.removeEventListener(FOCUS_OUT,this.focusOutHandler);
         }
      }
      
      public function get focusOwner() : IFocusDisplayObject
      {
         return this._focusOwner;
      }
      
      public function set focusOwner(value:IFocusDisplayObject) : void
      {
         this._focusOwner = value;
      }
      
      public function get isFocusEnabled() : Boolean
      {
         return this._isEnabled && this._isFocusEnabled;
      }
      
      public function set isFocusEnabled(value:Boolean) : void
      {
         if(!(this is IFocusDisplayObject))
         {
            throw new IllegalOperationError("Cannot enable focus on a component that does not implement feathers.core.IFocusDisplayObject");
         }
         if(this._isFocusEnabled == value)
         {
            return;
         }
         this._isFocusEnabled = value;
      }
      
      public function get nextTabFocus() : IFocusDisplayObject
      {
         return this._nextTabFocus;
      }
      
      public function set nextTabFocus(value:IFocusDisplayObject) : void
      {
         if(!(this is IFocusDisplayObject))
         {
            throw new IllegalOperationError("Cannot set next tab focus on a component that does not implement feathers.core.IFocusDisplayObject");
         }
         this._nextTabFocus = value;
      }
      
      public function get previousTabFocus() : IFocusDisplayObject
      {
         return this._previousTabFocus;
      }
      
      public function set previousTabFocus(value:IFocusDisplayObject) : void
      {
         if(!(this is IFocusDisplayObject))
         {
            throw new IllegalOperationError("Cannot set previous tab focus on a component that does not implement feathers.core.IFocusDisplayObject");
         }
         this._previousTabFocus = value;
      }
      
      public function get focusIndicatorSkin() : DisplayObject
      {
         return this._focusIndicatorSkin;
      }
      
      public function set focusIndicatorSkin(value:DisplayObject) : void
      {
         if(!(this is IFocusDisplayObject))
         {
            throw new IllegalOperationError("Cannot set focus indicator skin on a component that does not implement feathers.core.IFocusDisplayObject");
         }
         if(this._focusIndicatorSkin == value)
         {
            return;
         }
         if(Boolean(this._focusIndicatorSkin) && Boolean(this._focusIndicatorSkin.parent))
         {
            trace("FIXME");
         }
         this._focusIndicatorSkin = value;
         if(Boolean(this._focusIndicatorSkin))
         {
         }
         if(Boolean(this._focusManager) && this._focusManager.focus == this)
         {
            this.invalidate(INVALIDATION_FLAG_STYLES);
         }
      }
      
      public function get focusPadding() : Number
      {
         return this._focusPaddingTop;
      }
      
      public function set focusPadding(value:Number) : void
      {
         this.focusPaddingTop = value;
         this.focusPaddingRight = value;
         this.focusPaddingBottom = value;
         this.focusPaddingLeft = value;
      }
      
      public function get focusPaddingTop() : Number
      {
         return this._focusPaddingTop;
      }
      
      public function set focusPaddingTop(value:Number) : void
      {
         if(this._focusPaddingTop == value)
         {
            return;
         }
         this._focusPaddingTop = value;
         this.invalidate(INVALIDATION_FLAG_FOCUS);
      }
      
      public function get focusPaddingRight() : Number
      {
         return this._focusPaddingRight;
      }
      
      public function set focusPaddingRight(value:Number) : void
      {
         if(this._focusPaddingRight == value)
         {
            return;
         }
         this._focusPaddingRight = value;
         this.invalidate(INVALIDATION_FLAG_FOCUS);
      }
      
      public function get focusPaddingBottom() : Number
      {
         return this._focusPaddingBottom;
      }
      
      public function set focusPaddingBottom(value:Number) : void
      {
         if(this._focusPaddingBottom == value)
         {
            return;
         }
         this._focusPaddingBottom = value;
         this.invalidate(INVALIDATION_FLAG_FOCUS);
      }
      
      public function get focusPaddingLeft() : Number
      {
         return this._focusPaddingLeft;
      }
      
      public function set focusPaddingLeft(value:Number) : void
      {
         if(this._focusPaddingLeft == value)
         {
            return;
         }
         this._focusPaddingLeft = value;
         this.invalidate(INVALIDATION_FLAG_FOCUS);
      }
      
      public function get isCreated() : Boolean
      {
         return this._hasValidated;
      }
      
      public function get depth() : int
      {
         return this._depth;
      }
      
      public function invalidate(flag:String = "all") : void
      {
         var otherFlag:String = null;
         this.draw();
         var isAlreadyInvalid:Boolean = this.isInvalid();
         var isAlreadyDelayedInvalid:Boolean = false;
         if(this._isValidating)
         {
            var _loc5_:int = 0;
            var _loc6_:* = this._delayedInvalidationFlags;
            for(otherFlag in _loc6_)
            {
               isAlreadyDelayedInvalid = true;
            }
         }
         if(!flag || flag == INVALIDATION_FLAG_ALL)
         {
            if(this._isValidating)
            {
               this._delayedInvalidationFlags[INVALIDATION_FLAG_ALL] = true;
            }
            else
            {
               this._isAllInvalid = true;
            }
         }
         else if(this._isValidating)
         {
            this._delayedInvalidationFlags[flag] = true;
         }
         else if(flag != INVALIDATION_FLAG_ALL && !this._invalidationFlags.hasOwnProperty(flag))
         {
            this._invalidationFlags[flag] = true;
         }
         if(this._isValidating)
         {
            return;
         }
         if(isAlreadyInvalid)
         {
            return;
         }
         this._invalidateCount = 0;
      }
      
      public function validate() : void
      {
         var flag:String = null;
         if(this._isDisposed)
         {
            return;
         }
         if(!this._isInitialized)
         {
            this.initializeInternal();
         }
         if(!this.isInvalid())
         {
            return;
         }
         if(this._isValidating)
         {
            trace("FIXME");
            return;
         }
         this._isValidating = true;
         this.draw();
         for(flag in this._invalidationFlags)
         {
            delete this._invalidationFlags[flag];
         }
         this._isAllInvalid = false;
         for(flag in this._delayedInvalidationFlags)
         {
            if(flag == INVALIDATION_FLAG_ALL)
            {
               this._isAllInvalid = true;
            }
            else
            {
               this._invalidationFlags[flag] = true;
            }
            delete this._delayedInvalidationFlags[flag];
         }
         this._isValidating = false;
         if(!this._hasValidated)
         {
            this._hasValidated = true;
            this.dispatchEvent(new Event(CREATION_COMPLETE));
         }
      }
      
      protected function setSizeInternal(width:Number, height:Number, canInvalidate:Boolean) : Boolean
      {
         if(this.explicitWidth === this.explicitWidth)
         {
            width = this.explicitWidth;
         }
         else if(width < this._minWidth)
         {
            width = this._minWidth;
         }
         else if(width > this._maxWidth)
         {
            width = this._maxWidth;
         }
         if(this.explicitHeight === this.explicitHeight)
         {
            height = this.explicitHeight;
         }
         else if(height < this._minHeight)
         {
            height = this._minHeight;
         }
         else if(height > this._maxHeight)
         {
            height = this._maxHeight;
         }
         if(width !== width)
         {
            throw new ArgumentError(ILLEGAL_WIDTH_ERROR);
         }
         if(height !== height)
         {
            throw new ArgumentError(ILLEGAL_HEIGHT_ERROR);
         }
         var resized:Boolean = false;
         if(this.actualWidth != width)
         {
            this.actualWidth = width;
            this.refreshHitAreaX();
            resized = true;
         }
         if(this.actualHeight != height)
         {
            this.actualHeight = height;
            this.refreshHitAreaY();
            resized = true;
         }
         width = this.scaledActualWidth;
         height = this.scaledActualHeight;
         this.scaledActualWidth = this.actualWidth * Math.abs(this.scaleX);
         this.scaledActualHeight = this.actualHeight * Math.abs(this.scaleY);
         if(width != this.scaledActualWidth || height != this.scaledActualHeight)
         {
            resized = true;
         }
         if(resized)
         {
            if(canInvalidate)
            {
               this.invalidate(INVALIDATION_FLAG_SIZE);
            }
            this.dispatchEvent(new Event(Event.RESIZE));
         }
         return resized;
      }
      
      public function isInvalid(flag:String = null) : Boolean
      {
         if(this._isAllInvalid)
         {
            return true;
         }
         if(!flag)
         {
            var _loc2_:int = 0;
            var _loc3_:* = this._invalidationFlags;
            for(flag in _loc3_)
            {
               return true;
            }
            return false;
         }
         return this._invalidationFlags[flag];
      }
      
      protected function initialize() : void
      {
      }
      
      protected function draw() : void
      {
      }
      
      protected function setInvalidationFlag(flag:String) : void
      {
         if(this._invalidationFlags.hasOwnProperty(flag))
         {
            return;
         }
         this._invalidationFlags[flag] = true;
      }
      
      protected function clearInvalidationFlag(flag:String) : void
      {
         delete this._invalidationFlags[flag];
      }
      
      protected function refreshFocusIndicator() : void
      {
         if(Boolean(this._focusIndicatorSkin))
         {
            if(this._hasFocus && this._showFocus)
            {
               if(this._focusIndicatorSkin.parent != this)
               {
                  this.addChild(this._focusIndicatorSkin);
               }
               else
               {
                  this.setChildIndex(this._focusIndicatorSkin,this.numChildren - 1);
               }
            }
            else if(Boolean(this._focusIndicatorSkin.parent))
            {
            }
            this._focusIndicatorSkin.x = this._focusPaddingLeft;
            this._focusIndicatorSkin.y = this._focusPaddingTop;
            this._focusIndicatorSkin.width = this.actualWidth - this._focusPaddingLeft - this._focusPaddingRight;
            this._focusIndicatorSkin.height = this.actualHeight - this._focusPaddingTop - this._focusPaddingBottom;
         }
      }
      
      protected function refreshHitAreaX() : void
      {
      }
      
      protected function refreshHitAreaY() : void
      {
      }
      
      protected function initializeInternal() : void
      {
         if(this._isInitialized)
         {
            return;
         }
         this.initialize();
         this.invalidate();
         this._isInitialized = true;
         this.dispatchEvent(new Event(INITIALIZE));
      }
      
      protected function focusInHandler(event:Event) : void
      {
         this._hasFocus = true;
         this.invalidate(INVALIDATION_FLAG_FOCUS);
      }
      
      protected function focusOutHandler(event:Event) : void
      {
         this._hasFocus = false;
         this._showFocus = false;
         this.invalidate(INVALIDATION_FLAG_FOCUS);
      }
   }
}

