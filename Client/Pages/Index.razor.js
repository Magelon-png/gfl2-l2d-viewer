class L2dMotion {
    /**
     *
     * @param {string} file
     */
    constructor(file) {
        this.file = file;
    }
}

class L2dMotionDictionary {
    /**
     *
     * @param {Object.<string, L2dMotion[]>} motions
     */
    constructor(motions) {
        this.motions = motions;
    }
}

class L2dExpression {
    /**
     *
     * @param {string} name
     * @param {string} filePath
     */
    constructor(name, filePath) {
        this.name = name;
        this.filePath = filePath;
    }
}

class L2dModel {
    /**
     * 
     * @param {L2dMotionDictionary?} motions 
     * @param {L2dExpression[]?} expressions
     * @param {number} scale
     * @param {string[]} parameters
     * @param {boolean} editable
     */
    constructor(motions, expressions, scale, parameters, editable) {
        this.motions = motions;
        this.expressions = expressions;
        this.scale = scale;
        this.parameters = parameters;
        this.editable = editable;
    }
}

/**
 *
 * @type {Application}
 */
var l2dApp;

/**
 * @type {Live2DModel}
 */
var l2dModel;


/**
 *
 * @param {string} modelPath 
 * @param {boolean} editable
 * @returns {Promise<L2dModel>}
 */
export async function loadModel(modelPath, editable) {
    console.log(`Attempting to load L2D model from ${modelPath}`);

    l2dApp = new PIXI.Application({
        view: document.getElementById("canvas"),
        autoStart: true,
        resizeTo: window
    });

    l2dModel = await PIXI.live2d.Live2DModel.from(modelPath);

    l2dApp.stage.addChild(l2dModel);

    //Disable eye blinking if loaded as editable
    //If eye blinking is working => Cannot edit parameters
    if(editable === true) {
        l2dModel.internalModel.eyeBlink = undefined
    }

    l2dModel.interactive = false;

    l2dModel.autoInteract = false;
    

    let scale = Math.min(l2dApp.view.width / l2dModel.internalModel.width, 
        l2dApp.view.height / l2dModel.internalModel.height);

    scale = Math.round(scale * 10) / 10;
    
    l2dModel.scale.set(scale, scale);
    
    const returnValue = new L2dModel(l2dModel.internalModel.settings?.motions,
        l2dModel.internalModel.settings?.expressions, scale, 
        l2dModel.internalModel.coreModel._parameterIds,
        editable);
    
    console.log(JSON.stringify(returnValue));
    
    return returnValue;
}

/**
 * 
 * @param {string} expression
 */
export function setExpression(expression) {
    if(l2dApp && l2dModel) {
        l2dModel.expression(expression);
    }
}

/**
 * 
 * @param {boolean} enabled
 */
export function setEyeTracking(enabled)
{
    if(l2dApp && l2dModel) {
        l2dModel.interactive = enabled;

        l2dModel.autoInteract = enabled;
    }
}

/**
 * 
 * @param {string} motion
 */
export function setMotion(motion) {
    if(l2dModel && l2dApp) {
        l2dModel.motion(motion, 0);
    }
}

/**
 * 
 * @param {string} scale
 */
export function setScale(scale) {
    if(l2dModel && l2dApp) {
        l2dModel.scale.set(scale);
    }
}

/**
 * @description Edit one of the parameters on the model. Can only be used if the model is loaded as editable (EyeBlink = undefined)
 * @param {string} name
 * @param {string} value
 */
export function setParameter(name, value) {
    if(l2dModel && l2dApp) {
        l2dModel.internalModel.coreModel.setParameterValueById(name, value);
    }
}


//Dragging based on https://www.w3schools.com/howto/howto_js_draggable.asp
dragElement(document.getElementById("canvas"));

function dragElement(elmnt) {
    var pos1 = 0, pos2 = 0, pos3 = 0, pos4 = 0;
    if (document.getElementById(elmnt.id + "header")) {
        // if present, the header is where you move the DIV from:
        document.getElementById(elmnt.id + "header").onmousedown = dragMouseDown;
    } else {
        // otherwise, move the DIV from anywhere inside the DIV:
        elmnt.onmousedown = dragMouseDown;
    }

    function dragMouseDown(e) {
        e = e || window.event;
        e.preventDefault();
        // get the mouse cursor position at startup:
        pos3 = e.clientX;
        pos4 = e.clientY;
        document.onmouseup = closeDragElement;
        // call a function whenever the cursor moves:
        document.onmousemove = elementDrag;
    }

    function elementDrag(e) {
        e = e || window.event;
        e.preventDefault();
        // calculate the new cursor position:
        pos1 = pos3 - e.clientX;
        pos2 = pos4 - e.clientY;
        pos3 = e.clientX;
        pos4 = e.clientY;
        // set the element's new position:
        l2dModel.position.y = (l2dModel.position.y - pos2);
        l2dModel.position.x = (l2dModel.position.x - pos1) ;
    }

    function closeDragElement() {
        // stop moving when mouse button is released:
        document.onmouseup = null;
        document.onmousemove = null;
    }
}